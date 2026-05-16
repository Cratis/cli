// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Arc;

/// <summary>
/// Settings shared by all commands that connect to a Cratis Arc application.
/// </summary>
public class ArcSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the base URL of the Arc application.
    /// </summary>
    [CommandOption("--url <URL>")]
    [Description("Base URL of the Arc application (e.g. https://localhost:5001)")]
    public string? Url { get; set; }

    /// <summary>
    /// Resolves the effective base URL by checking the flag, environment variable, then default.
    /// </summary>
    /// <returns>The resolved base URL string.</returns>
#pragma warning disable CA1055 // URI string return type — string is intentional here for consistency with connection helpers
    public string ResolveUrl()
#pragma warning restore CA1055
    {
        if (!string.IsNullOrWhiteSpace(Url))
        {
            return Url.TrimEnd('/');
        }

        var envVar = Environment.GetEnvironmentVariable(ArcDefaults.UrlEnvVar);
        if (!string.IsNullOrWhiteSpace(envVar))
        {
            return envVar.TrimEnd('/');
        }

        return ArcDefaults.DefaultUrl;
    }
}
