// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Cratis.Cli.Commands.Chronicle.Auth;

/// <summary>
/// Authenticates a user via the resource owner password credentials flow and stores the session in the active context.
/// </summary>
[LlmDescription("Authenticates with the Chronicle server using username and password (password grant). Required before running commands on secured servers.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("login", "Log in as a user via the password grant flow", Branch = typeof(ChronicleBranch))]
[CliExample("chronicle", "login", "admin")]
[CliExample("chronicle", "login", "admin", "--secret", "P@ssw0rd!")]
[LlmOutputAdvice("plain", "Top-level command (not 'auth login'). Prompts for password interactively, or use --secret for non-interactive auth.")]
[LlmOption("<USERNAME>", "string", "The username to log in with (positional)")]
[LlmOption("--secret", "string", "Password for non-interactive login. If omitted, prompts interactively.")]
public class LoginCommand : AsyncCommand<LoginSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, LoginSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        string secret;
        if (!string.IsNullOrWhiteSpace(settings.Secret))
        {
            secret = settings.Secret;
        }
        else
        {
            try
            {
                secret = await AnsiConsole.PromptAsync(
                    new TextPrompt<string>("Password:")
                        .PromptStyle("dim")
                        .Secret());
            }
            catch (InvalidOperationException)
            {
                OutputFormatter.WriteError(format, "Interactive terminal required", "The login command requires an interactive terminal for secure password entry. Use --secret for non-interactive login.", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }
        }

        try
        {
            // Resolve without composing an existing (possibly expired) login token.
            const string srvLoginError = "Login does not support chronicle+srv servers: the resolved host cannot be bound to the stored token. Use a direct Chronicle server address.";
            var selectedServer = settings.ResolveServer();
            if (selectedServer.StartsWith("chronicle+srv://", StringComparison.OrdinalIgnoreCase))
            {
                OutputFormatter.WriteError(format, "Login failed", srvLoginError, ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            var connectionString = new ChronicleConnectionString(selectedServer);
            var config = CliConfiguration.Load();
            var ctx = config.GetCurrentContext();
            var contextServer = new ChronicleConnectionString(string.IsNullOrWhiteSpace(ctx.Server) ? "chronicle://localhost:35000" : ctx.Server);
            if (connectionString.IsSrv || contextServer.IsSrv)
            {
                OutputFormatter.WriteError(format, "Login failed", srvLoginError, ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            var tokenServer = ChronicleSettings.GetTokenServer(connectionString);
            if (connectionString.ServerAddresses.Count != 1 ||
                contextServer.ServerAddresses.Count != 1 ||
                !string.Equals(tokenServer, ChronicleSettings.GetTokenServer(contextServer), StringComparison.OrdinalIgnoreCase))
            {
                OutputFormatter.WriteError(format, "Login failed", "The login server differs from the active context's server (or uses multiple hosts). Create a context for this server with 'cratis context create <name> --server <url>' and switch with 'cratis context set <name>'.", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            // Chronicle serves the OAuth endpoint over TLS on the same port as gRPC.
            var address = connectionString.ServerAddress;
            var tokenEndpoint = new UriBuilder(Uri.UriSchemeHttps, address.Host, address.Port, "/connect/token").Uri;

            using var httpClient = CreateHttpClient(connectionString);
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = settings.Username,
                ["password"] = secret
            });

            using var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // The server's body may contain credentials; never print it.
                OutputFormatter.WriteError(format, "Login failed", $"Server returned {(int)response.StatusCode} ({response.StatusCode}).", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            // Bound reads even if the server streams an unlimited response body.
            var body = new byte[65537];
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var length = 0;
            while (length < body.Length)
            {
                var count = await stream.ReadAsync(body.AsMemory(length), cancellationToken);
                if (count == 0)
                {
                    break;
                }

                length += count;
            }

            if (length > 65536)
            {
                OutputFormatter.WriteError(format, "Login failed", "Server returned an oversized token response.", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            using var tokenResponse = JsonDocument.Parse(body.AsMemory(0, length));
            var root = tokenResponse.RootElement;
            if ((root.TryGetProperty("token_type", out var tokenType) &&
                 (tokenType.ValueKind != JsonValueKind.String || !string.Equals(tokenType.GetString(), "Bearer", StringComparison.OrdinalIgnoreCase))) ||
                !root.TryGetProperty("access_token", out var accessTokenProperty) ||
                accessTokenProperty.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(accessTokenProperty.GetString()) ||
                !root.TryGetProperty("expires_in", out var expiresInProperty) ||
                expiresInProperty.ValueKind != JsonValueKind.Number ||
                !expiresInProperty.TryGetInt64(out var expiresIn) ||
                expiresIn <= 60 || expiresIn > int.MaxValue)
            {
                OutputFormatter.WriteError(format, "Login failed", "Server did not return a usable access token and expiry.", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            var expiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

            // The token is usable only with the server that issued it.
            ctx.ClientId = null;
            ctx.ClientSecret = null;
            ctx.AccessToken = accessTokenProperty.GetString();
            ctx.TokenExpiry = expiry.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            ctx.TokenServer = tokenServer;
            ctx.LoggedInUser = settings.Username;
            config.Save();
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidServerAddress or MissingServerAddress)
        {
            OutputFormatter.WriteError(format, "Login failed", "Invalid Chronicle server connection string. Check the active context and --server value.", ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (JsonException)
        {
            OutputFormatter.WriteError(format, "Login failed", "Server did not return a usable access token and expiry.", ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (HttpRequestException ex)
        {
            OutputFormatter.WriteError(format, CliDefaults.CannotConnectMessage, ex.Message, ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }

        OutputFormatter.WriteMessage(format, $"Logged in as {settings.Username}.");
        return ExitCodes.Success;
    }

    /// <summary>
    /// Creates the HTTP client used to request a login token.
    /// </summary>
    /// <param name="connectionString">The server's TLS configuration.</param>
    /// <returns>An HTTP client for Chronicle's OAuth endpoint.</returns>
#pragma warning disable MA0039 // Do not write your own certificate validation method
    protected virtual HttpClient CreateHttpClient(ChronicleConnectionString connectionString)
    {
        var handler = new HttpClientHandler { CheckCertificateRevocationList = true };
        if (connectionString.SkipTlsValidation)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }
        else if (!string.IsNullOrWhiteSpace(connectionString.CertificatePath))
        {
            handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
                ValidateCertificate(certificate, errors, connectionString.CertificatePath, connectionString.CertificatePassword);
        }

        return new HttpClient(handler);
    }

    static bool ValidateCertificate(X509Certificate2? certificate, SslPolicyErrors errors, string certificatePath, string? password)
    {
        if (certificate is null || errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch) ||
            errors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            return false;
        }

        using var trusted = Path.GetExtension(certificatePath).Equals(".pfx", StringComparison.OrdinalIgnoreCase)
            ? X509CertificateLoader.LoadPkcs12FromFile(certificatePath, password)
            : X509CertificateLoader.LoadCertificateFromFile(certificatePath);
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(trusted);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return chain.Build(certificate);
    }
#pragma warning restore MA0039
}
