// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Holds packages in memory, optionally failing as an unreachable registry would.
/// </summary>
class InMemoryExtensionRegistry : IExtensionRegistry
{
    readonly List<RegistryPackage> _packages = [];

    public int Pulls { get; private set; }

    public ExtensionFailureKind? Fails { get; set; }

    public InMemoryExtensionRegistry With(params RegistryPackage[] packages)
    {
        _packages.AddRange(packages);
        return this;
    }

    public Task<IReadOnlyList<string>> Versions(string resolved, CancellationToken cancellationToken)
    {
        Fail(resolved);
        return Task.FromResult<IReadOnlyList<string>>([.. _packages.Where(package => package.Resolved == resolved).Select(package => package.Version)]);
    }

    public Task<RegistryPackage?> Pull(string resolved, string version, CancellationToken cancellationToken)
    {
        Fail(resolved);
        Pulls++;
        return Task.FromResult(_packages.FirstOrDefault(package => package.Resolved == resolved && package.Version == version));
    }

    void Fail(string resolved)
    {
        if (Fails is { } kind) throw new ExtensionFailure(kind, resolved, $"{kind} for {resolved}");
    }
}
