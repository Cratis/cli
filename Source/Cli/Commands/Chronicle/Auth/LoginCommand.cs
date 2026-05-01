// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Auth;

/// <summary>
/// Authenticates a user via the resource owner password credentials flow and stores the session in the active context.
/// </summary>
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
            var connectionString = new ChronicleConnectionString(settings.ResolveConnectionString());
            var disableTls = connectionString.DisableTls;
            var scheme = disableTls ? "http" : "https";
            var managementPort = settings.ResolveManagementPort();
            var tokenEndpoint = $"{scheme}://{connectionString.ServerAddress.Host}:{managementPort}/connect/token";

            using var handler = CreateHandler();
            using var httpClient = new HttpClient(handler);
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = settings.Username,
                ["password"] = secret
            });

            var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                OutputFormatter.WriteError(format, "Login failed", $"Server returned {(int)response.StatusCode}: {errorBody}", ExitCodes.AuthenticationErrorCode);
                return ExitCodes.AuthenticationError;
            }
        }
        catch (HttpRequestException ex)
        {
            OutputFormatter.WriteError(format, CliDefaults.CannotConnectMessage, ex.Message, ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();

        // Store logged-in user and clear any stale application credentials.
        ctx.ClientId = null;
        ctx.ClientSecret = null;
        ctx.AccessToken = null;
        ctx.TokenExpiry = null;
        ctx.LoggedInUser = settings.Username;
        config.Save();

        OutputFormatter.WriteMessage(format, $"Logged in as {settings.Username}.");
        return ExitCodes.Success;
    }

#pragma warning disable MA0039 // Do not write your own certificate validation method
    static HttpClientHandler CreateHandler()
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
    }
#pragma warning restore MA0039
}
