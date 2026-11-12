// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Defines a place packages come from: a registry, or a folder of vendored packages.
/// </summary>
public interface IExtensionRegistry
{
    /// <summary>
    /// Lists the releases of a package.
    /// </summary>
    /// <param name="resolved">The full reference.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The versions, in no particular order; empty when the registry does not have the package.</returns>
    /// <exception cref="ExtensionFailure">Thrown when the registry cannot be reached or refuses the credentials.</exception>
    Task<IReadOnlyList<string>> Versions(string resolved, CancellationToken cancellationToken);

    /// <summary>
    /// Pulls one release of a package, with its signature when it has one.
    /// </summary>
    /// <param name="resolved">The full reference.</param>
    /// <param name="version">The release.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The package, or <see langword="null"/> when the registry does not have it.</returns>
    /// <exception cref="ExtensionFailure">Thrown when the registry cannot be reached or refuses the credentials.</exception>
    Task<RegistryPackage?> Pull(string resolved, string version, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a place packages can be copied to.
/// </summary>
public interface IExtensionDestination
{
    /// <summary>
    /// Gets a description of the destination for messages.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Copies a package, replacing an earlier copy of the same digest.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> for the asynchronous operation.</returns>
    Task Push(RegistryPackage package, CancellationToken cancellationToken);
}
