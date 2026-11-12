// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.given;

/// <summary>
/// A project whose model names the Stripe extension, with the lock pinning it, the publisher allowed and its contract in the cache the compiler reads.
/// </summary>
[Collection(CliSpecsCollection.Name)]
public class a_restored_extension_project : Specification
{
    const string CacheVariable = ExtensionCache.EnvironmentVariable;

    string? _previousCache;

    /// <summary>Gets the temporary project.</summary>
    internal ExtensionProject Project { get; private set; } = null!;

    /// <summary>Gets the package the lock pins.</summary>
    protected RegistryPackage Pinned { get; private set; } = null!;

    /// <summary>
    /// Writes the model, restores the pinned package and points the compiler at the project's cache.
    /// </summary>
    /// <param name="model">The text of the model's file, or null for the project's default.</param>
    protected void Restore(string? model = null)
    {
        Project = new();
        Project.AllowPublisher();
        Pinned = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        Project.Registry.With(Pinned);
        Project.Lock("2", Pinned);
        if (model is not null) File.WriteAllText(Path.Combine(Project.Model, "payments.play"), model);
        _previousCache = Environment.GetEnvironmentVariable(CacheVariable);
        Environment.SetEnvironmentVariable(CacheVariable, Project.Cache.Root);
        Project.Service().Restore(Project.Workspace(), true, CancellationToken.None).GetAwaiter().GetResult();
    }

    void Destroy()
    {
        Environment.SetEnvironmentVariable(CacheVariable, _previousCache);
        Project?.Dispose();
    }
}
