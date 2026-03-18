// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli;

/// <summary>
/// Global settings shared by all CLI commands.
/// </summary>
public class GlobalSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the Chronicle server connection string.
    /// </summary>
    [CommandOption("--server <CONNECTION_STRING>")]
    [Description("Chronicle server connection string (e.g. chronicle://localhost:35000)")]
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets the output format.
    /// </summary>
    [CommandOption("-o|--output <FORMAT>")]
    [Description("Output format: json, text, plain, or json-compact (non-indented JSON)")]
    [DefaultValue(OutputFormats.Auto)]
    public string Output { get; set; } = OutputFormats.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether quiet mode is enabled.
    /// When enabled, only key identifiers are emitted, one per line, with no headers or decoration.
    /// </summary>
    [CommandOption("-q|--quiet")]
    [Description("Quiet mode: output only key identifiers, one per line. Suppresses messages and formatting.")]
    [DefaultValue(false)]
    public bool Quiet { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether confirmation prompts should be skipped.
    /// </summary>
    [CommandOption("-y|--yes")]
    [Description("Skip confirmation prompts (assume yes)")]
    [DefaultValue(false)]
    public bool Yes { get; set; }

    /// <summary>
    /// Gets or sets the management port for the HTTP API and token endpoint.
    /// </summary>
    [CommandOption("--management-port <PORT>")]
    [Description("Management port for the HTTP API and token endpoint (default: 8080)")]
    public int? ManagementPort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether debug output should be written to stderr.
    /// When enabled, prints resolved config path, context, connection string (credentials redacted), and RPC timing.
    /// </summary>
    [CommandOption("--debug")]
    [Description("Print debug information to stderr: resolved config path, context, connection string, and RPC timing")]
    [DefaultValue(false)]
    public bool Debug { get; set; }

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
    /// Resolves the effective output format, using auto-detection when set to "auto".
    /// </summary>
    /// <returns>The resolved output format name.</returns>
    public string ResolveOutputFormat()
    {
        if (Quiet)
        {
            return OutputFormats.Quiet;
        }

        if (string.Equals(Output, OutputFormats.JsonCompact, StringComparison.OrdinalIgnoreCase))
        {
            return OutputFormats.JsonCompact;
        }

        if (!string.Equals(Output, OutputFormats.Auto, StringComparison.OrdinalIgnoreCase))
        {
            return Output.ToLowerInvariant();
        }

        var noColor = Environment.GetEnvironmentVariable("NO_COLOR");
        if (noColor is not null)
        {
            return OutputFormats.Plain;
        }

        return Console.IsOutputRedirected ? OutputFormats.Json : OutputFormats.Text;
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
        var parsed = new ChronicleConnectionString(connectionString);
        if (!string.IsNullOrEmpty(parsed.Username))
        {
            return connectionString;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        if (!string.IsNullOrWhiteSpace(ctx.ClientId) && !string.IsNullOrWhiteSpace(ctx.ClientSecret))
        {
            return parsed.WithCredentials(ctx.ClientId, ctx.ClientSecret).ToString();
        }

        return connectionString;
    }
}
