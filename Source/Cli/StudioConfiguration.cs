// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// The Cratis Studio this machine builds for. Holds no credentials: the builder token and the MCP client secret are kept in
/// the operating system's credential store, under keys derived from the address and the organization.
/// </summary>
public sealed record StudioConfiguration
{
    /// <summary>Gets or sets the Studio address, such as https://cratis.studio.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the organization's name in Studio.</summary>
    public string Organization { get; set; } = string.Empty;

    /// <summary>Gets or sets the builder's identifier, given by Studio when it registered.</summary>
    public string? BuilderId { get; set; }

    /// <summary>Gets or sets what maintainers see the builder as.</summary>
    public string? BuilderName { get; set; }

    /// <summary>Gets or sets the address of Studio's MCP server, such as https://mcp.cratis.studio.</summary>
    public string? McpUrl { get; set; }

    /// <summary>Gets or sets the organization's MCP client id, which is its namespace and not a secret.</summary>
    public string? McpClientId { get; set; }

    /// <summary>Gets or sets whether the user explicitly enabled plaintext credential storage.</summary>
    public bool InsecureFileStore { get; set; }

    /// <summary>Gets the key the builder token is stored under.</summary>
    internal string BuilderTokenKey => $"studio-builder-{Key}";

    /// <summary>Gets the key the MCP client secret is stored under.</summary>
    internal string McpSecretKey => $"studio-mcp-{Key}";

    /// <summary>Gets a file name and a keychain account alike: the address and organization, hashed so no separator reaches a path.</summary>
    string Key => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{new Uri(Url).Authority.ToLowerInvariant()}/{Organization}")))[..32];
}
