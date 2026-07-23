// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Builds the <see cref="PrologueConfiguration"/> a <c>cratis-prologue.json</c> file holds from the values
/// entered in the Prologue setup wizard — the pure assembly logic, separated from the interactive prompting
/// so it can be verified without a terminal.
/// </summary>
public static class PrologueConfigurationBuilder
{
    /// <summary>
    /// The base path suggested for the API capture source.
    /// </summary>
    public const string DefaultApiBasePath = "/api";

    /// <summary>
    /// Builds the configuration for the values entered in the wizard.
    /// </summary>
    /// <param name="input">The values entered in the wizard.</param>
    /// <returns>The <see cref="PrologueConfiguration"/> ready to be written as <c>cratis-prologue.json</c>.</returns>
    public static PrologueConfiguration Build(PrologueWizardInput input)
    {
        var configuration = new PrologueConfiguration
        {
            Prologue = new PrologueOptions
            {
                PrologueId = input.PrologueId,
                Output = new OutputOptions
                {
                    Kind = input.Output.Kind,
                    Api = new ApiOptions { Endpoint = input.Output.ApiEndpoint },
                    Json = new JsonFileOptions { Directory = input.Output.JsonDirectory }
                },
                SqlServer = [.. input.SqlServer.Select(SqlServerFor)],
                Postgres = [.. input.Postgres.Select(PostgresFor)]
            }
        };

        if (input.OpenTelemetry is not null)
        {
            configuration.Prologue.OpenTelemetry = OpenTelemetryFor(input.OpenTelemetry);
        }

        if (input.Api is not null)
        {
            configuration.ReverseProxy = ReverseProxyFor(input.Api);
        }

        return configuration;
    }

    /// <summary>
    /// Normalizes a base path entered in the wizard — a leading slash, no trailing slash, falling back to
    /// <see cref="DefaultApiBasePath"/> when empty.
    /// </summary>
    /// <param name="basePath">The base path as entered.</param>
    /// <returns>The normalized base path.</returns>
    public static string NormalizeBasePath(string? basePath)
    {
        var trimmed = basePath?.Trim().TrimEnd('/') ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return DefaultApiBasePath;
        }

        return trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
    }

    /// <summary>
    /// Parses a comma-separated list entered in the wizard into its trimmed, non-empty values.
    /// </summary>
    /// <param name="value">The comma-separated value as entered; may be empty.</param>
    /// <returns>The parsed values.</returns>
    public static IReadOnlyList<string> ParseList(string? value) =>
        value?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];

    static SqlServerOptions SqlServerFor(SqlServerSourceInput source) => new()
    {
        Name = source.Name,
        ConnectionString = source.ConnectionString,
        Tables = [.. source.Tables]
    };

    static PostgresOptions PostgresFor(PostgresSourceInput source) => new()
    {
        Name = source.Name,
        ConnectionString = source.ConnectionString
    };

    static OpenTelemetryOptions OpenTelemetryFor(OpenTelemetrySourceInput source) => new()
    {
        Enabled = true,
        ServiceNames = [.. source.ServiceNames],
        AttributeKeys = [.. source.AttributeKeys],
        Upstream = new UpstreamOptions { Http = source.UpstreamHttp, Grpc = source.UpstreamGrpc }
    };

    static JsonObject ReverseProxyFor(ApiSourceInput api)
    {
        // The extractor's HTTP capture is a YARP reverse proxy in front of the system being captured; the
        // section follows YARP's own schema, so it is raw JSON on the configuration.
        var basePath = NormalizeBasePath(api.BasePath);
        return new JsonObject
        {
            ["Routes"] = new JsonObject
            {
                ["monitored"] = new JsonObject
                {
                    ["ClusterId"] = "monitored",
                    ["Match"] = new JsonObject { ["Path"] = $"{basePath}/{{**catch-all}}" }
                }
            },
            ["Clusters"] = new JsonObject
            {
                ["monitored"] = new JsonObject
                {
                    ["Destinations"] = new JsonObject
                    {
                        ["primary"] = new JsonObject { ["Address"] = api.Destination }
                    }
                }
            }
        };
    }
}
