// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Cli.Commands.Chronicle.Workbench;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Settings shared by all commands that connect to a Chronicle server.
/// </summary>
public class ChronicleSettings : GlobalSettings
{
    static int _legacyWarningReported;
    static int _tokenMismatchReported;

    /// <summary>
    /// Gets or sets the Chronicle server connection string.
    /// </summary>
    [CommandOption("--server <CONNECTION_STRING>")]
    [Description("Chronicle server connection string (e.g. chronicle://localhost:35000)")]
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets whether the interceptor already reported an expired login.
    /// </summary>
    internal bool LoginExpiredReported { get; set; }

    /// <summary>
    /// Gets or sets whether the interceptor already reported a connection resolution error.
    /// </summary>
    internal bool ConnectionResolutionReported { get; set; }

    /// <summary>
    /// Gets a value indicating whether a legacy login needs to be renewed.
    /// </summary>
    internal bool LegacyLoginNeedsRefresh { get; private set; }

    /// <summary>
    /// Resolves the effective connection string by checking flag, environment variable, current context, then default.
    /// When the resolved connection string has no embedded credentials, client credentials from the context are composed in.
    /// </summary>
    /// <returns>The resolved connection string.</returns>
    public string ResolveConnectionString() => ComposeCredentials(ResolveServer(), Debug);

    /// <summary>
    /// Normalizes the server address for comparison with the issuer of a login token.
    /// </summary>
    /// <param name="connectionString">The connection string to inspect.</param>
    /// <returns>The normalized server host and port.</returns>
    internal static string GetTokenServer(ChronicleConnectionString connectionString) =>
        $"{connectionString.ServerAddress.Host.ToLowerInvariant()}:{connectionString.ServerAddress.Port}";

    /// <summary>
    /// Returns true when the connection string already contains authentication — either
    /// embedded credentials (chronicle://user:pass@host) or an API key query parameter.
    /// </summary>
    /// <param name="connectionString">The Chronicle connection string to inspect.</param>
    internal static bool HasEmbeddedAuth(string connectionString)
    {
        const string scheme = "chronicle://";
        if (!connectionString.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var afterScheme = connectionString[scheme.Length..];
        var queryStart = afterScheme.IndexOf('?');
        var hostPart = queryStart >= 0 ? afterScheme[..queryStart] : afterScheme;
        return hostPart.Contains('@') || connectionString.Contains("apiKey=", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resets process-wide warning flags for isolated specifications.
    /// </summary>
    internal static void ResetWarningsForSpecs()
    {
        Interlocked.Exchange(ref _legacyWarningReported, 0);
        Interlocked.Exchange(ref _tokenMismatchReported, 0);
    }

    /// <summary>
    /// Resolves the selected server without composing context credentials (used by login).
    /// </summary>
    /// <returns>The connection string for the selected server.</returns>
    internal string ResolveServer()
    {
        string connectionString;

        if (!string.IsNullOrWhiteSpace(Server))
        {
            connectionString = Server;
        }
        else
        {
            var envVar = Environment.GetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar);
            if (!string.IsNullOrWhiteSpace(envVar))
            {
                connectionString = envVar;
            }
            else
            {
                var config = CliConfiguration.Load();
                var ctx = config.GetCurrentContext();
                connectionString = !string.IsNullOrWhiteSpace(ctx.Server)
                    ? ctx.Server
                    : "chronicle://localhost:35000";
            }
        }

        return connectionString;
    }

    static bool IsTokenValid(string? tokenExpiry)
    {
        if (string.IsNullOrWhiteSpace(tokenExpiry))
        {
            return false;
        }

        return DateTimeOffset.TryParse(tokenExpiry, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiry) &&
               expiry > DateTimeOffset.UtcNow.AddMinutes(1);
    }

    static string AppendApiKey(string connectionString, string apiKey)
    {
        var separator = connectionString.Contains('?') ? "&" : "?";
        return $"{connectionString}{separator}apiKey={Uri.EscapeDataString(apiKey)}";
    }

    static string InsertCredentials(string connectionString, string clientId, string clientSecret)
    {
        const string scheme = "chronicle://";
        if (!connectionString.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var encodedId = Uri.EscapeDataString(clientId);
        var encodedSecret = Uri.EscapeDataString(clientSecret);
        return $"{scheme}{encodedId}:{encodedSecret}@{connectionString[scheme.Length..]}";
    }

    string ComposeCredentials(string connectionString, bool debug)
    {
        // Embedded credentials on the selected server take precedence over context credentials.
        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();

        // Never send a login token to another host, including failover hosts.
        var serverMatchesToken = string.IsNullOrWhiteSpace(ctx.TokenServer);
        if (!serverMatchesToken)
        {
            var selectedServer = new ChronicleConnectionString(connectionString);
            serverMatchesToken = !selectedServer.IsSrv && selectedServer.ServerAddresses.All(address =>
                string.Equals($"{address.Host.ToLowerInvariant()}:{address.Port}", ctx.TokenServer, StringComparison.OrdinalIgnoreCase));
        }

        if (!serverMatchesToken && debug && this is not WorkbenchSettings && Interlocked.Exchange(ref _tokenMismatchReported, 1) == 0)
        {
            Console.Error.WriteLine($"[debug] stored login token was not used because it belongs to {ctx.TokenServer}.");
        }

        if (HasEmbeddedAuth(connectionString))
        {
            return connectionString;
        }

        // 1. Cached login token (from 'cratis chronicle login').
        if (!string.IsNullOrWhiteSpace(ctx.TokenServer))
        {
            if (serverMatchesToken && !string.IsNullOrWhiteSpace(ctx.AccessToken) && IsTokenValid(ctx.TokenExpiry))
            {
                return AppendApiKey(connectionString, ctx.AccessToken);
            }

            if (serverMatchesToken && !string.IsNullOrWhiteSpace(ctx.LoggedInUser))
            {
                throw new LoginSessionExpired(ctx.LoggedInUser);
            }
        }
        else if (!string.IsNullOrWhiteSpace(ctx.LoggedInUser))
        {
            if (ctx.AccessToken is null && ctx.TokenExpiry is null)
            {
                LegacyLoginNeedsRefresh = true;
                if (this is not WorkbenchSettings && Interlocked.Exchange(ref _legacyWarningReported, 1) == 0)
                {
                    Console.Error.WriteLine("Warning: legacy login has no saved token; run 'cratis chronicle login' again.");
                }
            }
            else
            {
                throw new LoginSessionExpired(ctx.LoggedInUser);
            }
        }

        // 2. Service account credentials stored in context.
        if (!string.IsNullOrWhiteSpace(ctx.ClientId) && !string.IsNullOrWhiteSpace(ctx.ClientSecret))
        {
            return InsertCredentials(connectionString, ctx.ClientId, ctx.ClientSecret);
        }

        // 3. Fall back to built-in development credentials (local Chronicle servers).
        return InsertCredentials(connectionString, ChronicleConnectionString.DevelopmentClient, ChronicleConnectionString.DevelopmentClientSecret);
    }
}
