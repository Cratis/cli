// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            var server = new Uri(settings.ResolveServer());

            // Chronicle serves the OAuth endpoint over TLS on the same port as gRPC. Login must
            // resolve the server without requiring an existing (possibly expired) login token.
            var tokenEndpoint = new UriBuilder(Uri.UriSchemeHttps, server.Host, server.Port, "/connect/token").Uri;

            using var httpClient = CreateHttpClient();
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

            using var tokenResponse = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = tokenResponse.RootElement;
            if (!root.TryGetProperty("access_token", out var accessTokenProperty) ||
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
            var config = CliConfiguration.Load();
            var ctx = config.GetCurrentContext();

            // --server selects the endpoint, but credentials belong to the active context,
            // just as they do for all other Chronicle commands.
            ctx.ClientId = null;
            ctx.ClientSecret = null;
            ctx.AccessToken = accessTokenProperty.GetString();
            ctx.TokenExpiry = expiry.ToString("O");
            ctx.LoggedInUser = settings.Username;
            config.Save();
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
    /// <returns>An HTTP client for Chronicle's OAuth endpoint.</returns>
#pragma warning disable MA0039 // Do not write your own certificate validation method
    protected virtual HttpClient CreateHttpClient() => new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });
#pragma warning restore MA0039
}
