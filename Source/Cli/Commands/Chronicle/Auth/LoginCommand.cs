// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Auth;

/// <summary>
/// Authenticates the CLI as an application using the client_credentials OAuth flow and stores the credentials in the active context.
/// </summary>
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
                secret = AnsiConsole.Prompt(
                    new TextPrompt<string>("Client secret:")
                        .PromptStyle("dim")
                        .Secret());
            }
            catch (InvalidOperationException)
            {
                OutputFormatter.WriteError(format, "Interactive terminal required", "The login command requires an interactive terminal for secure secret entry. Use --secret for non-interactive login.", ExitCodes.AuthenticationErrorCode);
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
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId,
                ["client_secret"] = secret
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

        // Clear any old password-grant session and store client credentials.
        ctx.AccessToken = null;
        ctx.TokenExpiry = null;
        ctx.LoggedInUser = null;
        ctx.ClientId = settings.ClientId;
        ctx.ClientSecret = secret;
        config.Save();

        OutputFormatter.WriteMessage(format, $"Logged in as application '{settings.ClientId}'.");
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
