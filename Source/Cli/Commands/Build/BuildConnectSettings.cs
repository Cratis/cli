// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Options for connecting this machine to Studio as a builder.
/// </summary>
public sealed class BuildConnectSettings : GlobalSettings
{
    /// <summary>The environment variable the organization's MCP client secret can be given in.</summary>
    public const string McpSecretVariable = "CRATIS_STUDIO_MCP_SECRET";

    /// <summary>
    /// Gets or sets the Studio address.
    /// </summary>
    [CommandOption("--studio <URL>")]
    [Description("The Studio address, such as https://cratis.studio.")]
    public string? Studio { get; set; }

    /// <summary>
    /// Gets or sets the organization.
    /// </summary>
    [CommandOption("--organization <NAME>")]
    [Description("The organization's name in Studio, as shown in its builder address.")]
    public string? Organization { get; set; }

    /// <summary>
    /// Gets or sets what maintainers see the builder as.
    /// </summary>
    [CommandOption("--name <NAME>")]
    [Description("What maintainers see this builder as. Defaults to the machine's name.")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the organization's MCP client id.
    /// </summary>
    [CommandOption("--mcp-client-id <ID>")]
    [Description("The organization's MCP client id (its namespace), from Settings > MCP, to give agents Studio's tools and to fetch context. The secret is read from CRATIS_STUDIO_MCP_SECRET or asked for; it is never taken on the command line.")]
    public string? McpClientId { get; set; }

    /// <summary>
    /// Gets or sets the address of Studio's MCP server.
    /// </summary>
    [CommandOption("--mcp-url <URL>")]
    [Description("The address of Studio's MCP server. Defaults to the one Studio names, or https://mcp.cratis.studio.")]
    public string? McpUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to register again.
    /// </summary>
    [CommandOption("--force")]
    [Description("Register this machine again even when it is already registered, for example after it was removed in Studio.")]
    public bool Force { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to return without waiting for approval.
    /// </summary>
    [CommandOption("--no-wait")]
    [Description("Return once registered, without waiting for a maintainer to approve the builder.")]
    public bool NoWait { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether plaintext credential storage is allowed.
    /// </summary>
    [CommandOption("--insecure-file-store")]
    [Description("Store the builder token in a 0600 file instead of the operating system's credential store.")]
    public bool InsecureFileStore { get; set; }
}
