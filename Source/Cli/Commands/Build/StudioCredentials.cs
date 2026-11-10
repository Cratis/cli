// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The credentials this machine uses with Studio, kept in the operating system's credential store and never in the CLI's
/// configuration, an order, a prompt or a log: the builder token this machine created, and the organization's MCP client
/// secret that gives agents Studio's tools.
/// </summary>
/// <param name="secrets">The credential store.</param>
internal sealed class StudioCredentials(IDirectSecretStore secrets)
{
    /// <summary>
    /// Creates a new builder token: 256 random bits as URL-safe base64, 43 characters.
    /// </summary>
    /// <returns>The token.</returns>
    public static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// Opens the credential store the connection uses.
    /// </summary>
    /// <param name="studio">The Studio connection.</param>
    /// <returns>The credentials.</returns>
    public static StudioCredentials For(StudioConfiguration studio) =>
        new(DirectSecretStores.Select(studio.InsecureFileStore, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));

    /// <summary>
    /// Reads the builder token.
    /// </summary>
    /// <param name="studio">The Studio connection.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The token, or <see langword="null"/> when none is stored.</returns>
    public Task<string?> BuilderToken(StudioConfiguration studio, CancellationToken cancellationToken) => secrets.Read(studio.BuilderTokenKey, cancellationToken);

    /// <summary>
    /// Stores the builder token.
    /// </summary>
    /// <param name="studio">The Studio connection.</param>
    /// <param name="token">The token.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the token is stored.</returns>
    public Task SaveBuilderToken(StudioConfiguration studio, string token, CancellationToken cancellationToken) => secrets.Write(studio.BuilderTokenKey, token, cancellationToken);

    /// <summary>
    /// Reads the MCP client secret.
    /// </summary>
    /// <param name="studio">The Studio connection.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The secret, or <see langword="null"/> when none is stored.</returns>
    public Task<string?> McpSecret(StudioConfiguration studio, CancellationToken cancellationToken) => secrets.Read(studio.McpSecretKey, cancellationToken);

    /// <summary>
    /// Stores the MCP client secret.
    /// </summary>
    /// <param name="studio">The Studio connection.</param>
    /// <param name="secret">The secret.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the secret is stored.</returns>
    public Task SaveMcpSecret(StudioConfiguration studio, string secret, CancellationToken cancellationToken) => secrets.Write(studio.McpSecretKey, secret, cancellationToken);
}
