// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Settings shared by all commands that connect to a Chronicle server.
/// </summary>
public class ChronicleSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the Chronicle server connection string.
    /// </summary>
    [CommandOption("--server <CONNECTION_STRING>")]
    [Description("Chronicle server connection string (e.g. chronicle://localhost:35000)")]
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets the management port for the HTTP API and token endpoint.
    /// </summary>
    [CommandOption("--management-port <PORT>")]
    [Description("Management port for the HTTP API and token endpoint (default: 8080)")]
    public int? ManagementPort { get; set; }

    /// <summary>
    /// Resolves the effective connection string by checking flag, environment variable, current context, then default.
    /// When the resolved connection string has no embedded credentials, client credentials from the context are composed in.
    /// </summary>
    /// <returns>The resolved connection string.</returns>
    public string ResolveConnectionString()
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
                if (!string.IsNullOrWhiteSpace(ctx.Server))
                {
                    connectionString = ctx.Server;
                }
                else
                {
                    connectionString = $"chronicle://{ChronicleConnectionString.DevelopmentClient}:{ChronicleConnectionString.DevelopmentClientSecret}@localhost:35000/?disableTls=true";
                }
            }
        }

        return ComposeCredentials(connectionString);
    }

    /// <summary>
    /// Resolves the effective management port by checking flag, environment variable, current context, then default.
    /// </summary>
    /// <returns>The resolved management port.</returns>
    public int ResolveManagementPort()
    {
        if (ManagementPort.HasValue)
        {
            return ManagementPort.Value;
        }

        var envVar = Environment.GetEnvironmentVariable(CliDefaults.ManagementPortEnvVar);
        if (!string.IsNullOrWhiteSpace(envVar) && int.TryParse(envVar, out var envPort))
        {
            return envPort;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();

        return ctx.ManagementPort ?? CliDefaults.DefaultManagementPort;
    }

    static string ComposeCredentials(string connectionString)
    {
        // Avoid calling the ChronicleConnectionString constructor here — it throws when no
        // auth is present, and the CLR may leave the object in a partially-initialized state
        // that makes the catch-and-continue pattern unreliable.
        // Instead we inspect the URL string directly: embedded credentials appear as
        // "chronicle://user:pass@host" and an API key as "?apiKey=" or "&apiKey=".
        if (HasEmbeddedAuth(connectionString))
        {
            return connectionString;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();

        // 1. Cached login token (from 'cratis chronicle login').
        if (!string.IsNullOrWhiteSpace(ctx.AccessToken) && IsTokenValid(ctx.TokenExpiry))
        {
            return AppendApiKey(connectionString, ctx.AccessToken);
        }

        // 2. Service account credentials stored in context.
        if (!string.IsNullOrWhiteSpace(ctx.ClientId) && !string.IsNullOrWhiteSpace(ctx.ClientSecret))
        {
            return InsertCredentials(connectionString, ctx.ClientId, ctx.ClientSecret);
        }

        // 3. Fall back to built-in development credentials (local Chronicle servers).
        return InsertCredentials(connectionString, ChronicleConnectionString.DevelopmentClient, ChronicleConnectionString.DevelopmentClientSecret);
    }

    /// <summary>
    /// Returns true when the connection string already contains authentication — either
    /// embedded credentials (chronicle://user:pass@host) or an API key query parameter.
    /// </summary>
    /// <param name="connectionString">The Chronicle connection string to inspect.</param>
    static bool HasEmbeddedAuth(string connectionString)
    {
        const string scheme = "chronicle://";
        if (!connectionString.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var afterScheme = connectionString[scheme.Length..];

        // Embedded credentials: "user:pass@host..."
        var queryStart = afterScheme.IndexOf('?');
        var hostPart = queryStart >= 0 ? afterScheme[..queryStart] : afterScheme;
        if (hostPart.Contains('@'))
        {
            return true;
        }

        // API key query parameter.
        if (connectionString.Contains("apiKey=", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    static bool IsTokenValid(string? tokenExpiry)
    {
        if (string.IsNullOrWhiteSpace(tokenExpiry))
        {
            return false;
        }

        return DateTimeOffset.TryParse(tokenExpiry, out var expiry) && expiry > DateTimeOffset.UtcNow.AddMinutes(1);
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
}
