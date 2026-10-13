// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
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
    public override async Task<int> ExecuteAsync(CommandContext context, LoginSettings settings, CancellationToken cancellationToken)
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
            if (connectionString.IsSrv)
            {
                OutputFormatter.WriteError(format, "Login failed", srvLoginError, ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            if (connectionString.ServerAddresses.Count != 1)
            {
                OutputFormatter.WriteError(format, "Login failed", "Login requires a single Chronicle server address; multiple hosts cannot be bound to a stored token.", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            var tokenServer = ChronicleSettings.GetTokenServer(connectionString);

            // The context may point elsewhere (including SRV or multiple hosts); only the login target must be bindable.
            var loginServerDiffersFromContext = true;
            try
            {
                var contextServer = new ChronicleConnectionString(string.IsNullOrWhiteSpace(ctx.Server) ? "chronicle://localhost:35000" : ctx.Server);
                loginServerDiffersFromContext = contextServer.IsSrv || contextServer.ServerAddresses.Count != 1 ||
                    !string.Equals(tokenServer, ChronicleSettings.GetTokenServer(contextServer), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidServerAddress or MissingServerAddress)
            {
                // An invalid context server must not prevent an explicit, valid --server login.
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

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint) { Content = content };
            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestTimeout.CancelAfter(httpClient.Timeout);
            var body = new byte[65537];
            var length = 0;
            try
            {
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestTimeout.Token);

                if ((int)response.StatusCode is >= 300 and < 400)
                {
                    OutputFormatter.WriteError(format, "Login failed", "Server redirected the login request; redirects are not allowed.", ExitCodes.AuthenticationErrorCode);
                    return ExitCodes.AuthenticationError;
                }

                if (!response.IsSuccessStatusCode)
                {
                    // The server's body may contain credentials; never print it.
                    OutputFormatter.WriteError(format, "Login failed", $"Server returned {(int)response.StatusCode} ({response.StatusCode}).", ExitCodes.AuthenticationErrorCode);
                    return ExitCodes.AuthenticationError;
                }

                // Bound both the response size and the time allowed to stream its body.
                await using var stream = await response.Content.ReadAsStreamAsync(requestTimeout.Token);
                while (length < body.Length)
                {
                    var count = await stream.ReadAsync(body.AsMemory(length), requestTimeout.Token);
                    if (count == 0)
                    {
                        break;
                    }

                    length += count;
                }
            }
            catch (IOException ex)
            {
                OutputFormatter.WriteError(format, CliDefaults.CannotConnectMessage, ex.Message, ExitCodes.ConnectionErrorCode);
                return ExitCodes.ConnectionError;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && requestTimeout.IsCancellationRequested)
            {
                OutputFormatter.WriteError(format, CliDefaults.CannotConnectMessage, "Login request timed out.", ExitCodes.ConnectionErrorCode);
                return ExitCodes.ConnectionError;
            }

            if (length > 65536)
            {
                OutputFormatter.WriteError(format, "Login failed", "Server returned an oversized token response.", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            JsonDocument tokenResponse;
            try
            {
                tokenResponse = JsonDocument.Parse(body.AsMemory(0, length));
            }
            catch (JsonException)
            {
                OutputFormatter.WriteError(format, "Login failed", "Server did not return a usable access token and expiry.", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            using var parsedTokenResponse = tokenResponse;
            var root = parsedTokenResponse.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                (root.TryGetProperty("token_type", out var tokenType) &&
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
            try
            {
                config.Save();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                OutputFormatter.WriteError(format, "Login failed", "Could not save login to the CLI configuration.", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }

            if (loginServerDiffersFromContext && format == OutputFormats.Table)
            {
                await Console.Error.WriteLineAsync($"Note: this token will only be used for {tokenServer} (for example, with the same --server).");
            }
        }
        catch (CertificateDoesNotExist)
        {
            OutputFormatter.WriteError(format, "Login failed", "The configured client certificate file does not exist.", ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (Exception ex) when (ex is InvalidCertificateOrPassword or CryptographicException)
        {
            OutputFormatter.WriteError(format, "Login failed", "The configured client certificate is invalid or its password is incorrect. Use a PKCS#12 client certificate.", ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidServerAddress or MissingServerAddress)
        {
            OutputFormatter.WriteError(format, "Login failed", "Invalid Chronicle server connection string. Check the active context and --server value.", ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            ChronicleCommand<ChronicleSettings>.ReportConnectionResolutionError(format, ex);
            return ExitCodes.ValidationError;
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
    protected virtual HttpClient CreateHttpClient(ChronicleConnectionString connectionString)
    {
        var certificate = !string.IsNullOrEmpty(connectionString.CertificatePath)
            ? CertificateLoader.LoadCertificate(connectionString.CertificatePath, connectionString.CertificatePassword)
            : null;
        try
        {
            var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
            if (certificate is not null)
            {
                handler.SslOptions.ClientCertificates = [certificate];
            }

            handler.SslOptions.RemoteCertificateValidationCallback = CertificateLoader.CreateServerCertificateValidationCallback(
                connectionString.SkipTlsValidation, certificate?.GetCertHashString());

            return new HttpClient(certificate is null ? handler : new CertificateLifetime(handler, certificate));
        }
        catch
        {
            certificate?.Dispose();
            throw;
        }
    }

    sealed class CertificateLifetime(HttpMessageHandler handler, X509Certificate2 certificate) : DelegatingHandler(handler)
    {
        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                if (disposing)
                {
                    certificate.Dispose();
                }
            }
        }
    }
}
