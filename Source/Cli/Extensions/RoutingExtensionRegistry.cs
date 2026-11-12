// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Routes a full reference to the registry for its channel: <c language="csharp">npm:</c> references to npm, everything else to OCI.
/// </summary>
/// <param name="oci">The OCI registry.</param>
/// <param name="npm">The npm registry.</param>
public class RoutingExtensionRegistry(IExtensionRegistry oci, IExtensionRegistry npm) : IExtensionRegistry
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> Versions(string resolved, CancellationToken cancellationToken) => For(resolved).Versions(resolved, cancellationToken);

    /// <inheritdoc/>
    public Task<RegistryPackage?> Pull(string resolved, string version, CancellationToken cancellationToken) => For(resolved).Pull(resolved, version, cancellationToken);

    IExtensionRegistry For(string resolved) => resolved.StartsWith("npm:", StringComparison.Ordinal) ? npm : oci;
}
