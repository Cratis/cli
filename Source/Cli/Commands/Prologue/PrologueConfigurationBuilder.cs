// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Builds the <see cref="PrologueConfiguration"/> a <c language="csharp">cratis-prologue.json</c> file holds from the values
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
    /// The reverse-proxy route and cluster the default service's API capture uses.
    /// </summary>
    public const string DefaultCluster = "monitored";

    /// <summary>
    /// Builds the configuration for the values entered in the wizard.
    /// </summary>
    /// <param name="input">The values entered in the wizard.</param>
    /// <returns>The <see cref="PrologueConfiguration"/> ready to be written as <c language="csharp">cratis-prologue.json</c>.</returns>
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
                Postgres = [.. input.Postgres.Select(PostgresFor)],
                Services = input.NamedServices.ToDictionary(service => service.Name, ServiceFor, StringComparer.OrdinalIgnoreCase)
            }
        };

        if (input.OpenTelemetry is not null)
        {
            configuration.Prologue.OpenTelemetry = OpenTelemetryFor(input.OpenTelemetry);
        }

        // The default service's API keeps the "monitored" route and cluster; each named service's API gets a route
        // and cluster named after the service, which is how the extractor attributes its commands to that service.
        var apis = new List<(string Cluster, ApiSourceInput Api)>();
        if (input.Api is not null)
        {
            apis.Add((DefaultCluster, input.Api));
        }

        apis.AddRange(input.NamedServices.Where(service => service.Api is not null).Select(service => (service.Name, service.Api!)));
        if (apis.Count > 0)
        {
            configuration.ReverseProxy = ReverseProxyFor(apis);
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

    /// <summary>
    /// Determines whether a name entered in the wizard can name a service — letters, digits, <c language="csharp">-</c>
    /// and <c language="csharp">_</c> only, since it also names the service's reverse-proxy route, cluster and
    /// capture files, and not already taken by another service.
    /// </summary>
    /// <param name="name">The name as entered.</param>
    /// <param name="existing">The names of the services entered so far.</param>
    /// <returns>True when the name can be used.</returns>
    public static bool IsValidServiceName(string? name, IEnumerable<string> existing)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        return trimmed.Length > 0 &&
            !string.Equals(trimmed, DefaultCluster, StringComparison.OrdinalIgnoreCase) &&
            trimmed.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_') &&
            !existing.Contains(trimmed, StringComparer.OrdinalIgnoreCase);
    }

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

    static ServiceOptions ServiceFor(ServiceSourceInput service) => new()
    {
        SqlServer = [.. service.SqlServer.Select(SqlServerFor)],
        Postgres = [.. service.Postgres.Select(PostgresFor)],
        TelemetryServiceNames = [.. service.TelemetryServiceNames]
    };

    static OpenTelemetryOptions OpenTelemetryFor(OpenTelemetrySourceInput source) => new()
    {
        Enabled = true,
        ServiceNames = [.. source.ServiceNames],
        AttributeKeys = [.. source.AttributeKeys],
        Upstream = new UpstreamOptions { Http = source.UpstreamHttp, Grpc = source.UpstreamGrpc }
    };

    static JsonObject ReverseProxyFor(IReadOnlyList<(string Cluster, ApiSourceInput Api)> apis)
    {
        // The extractor's HTTP capture is a YARP reverse proxy in front of the system being captured; the
        // section follows YARP's own schema, so it is raw JSON on the configuration.
        var routes = new JsonObject();
        var clusters = new JsonObject();
        foreach (var (cluster, api) in apis)
        {
            routes[cluster] = new JsonObject
            {
                ["ClusterId"] = cluster,
                ["Match"] = new JsonObject { ["Path"] = $"{NormalizeBasePath(api.BasePath)}/{{**catch-all}}" }
            };
            clusters[cluster] = new JsonObject
            {
                ["Destinations"] = new JsonObject
                {
                    ["primary"] = new JsonObject { ["Address"] = api.Destination }
                }
            };
        }

        return new JsonObject { ["Routes"] = routes, ["Clusters"] = clusters };
    }
}
