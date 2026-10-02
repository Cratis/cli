// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Reads the Screenplay documents a built application embeds - in its own assembly and in the assemblies it references.
/// </summary>
/// <remarks>
/// <para>
/// Only manifest resources are read, never a type, so the application's dependencies do not have to resolve and
/// nothing of it runs. An assembly that embeds documents is loaded from a copy of its bytes into a load context of
/// its own, which keeps the files free for the next build and keeps the application's assemblies from meeting the
/// CLI's.
/// </para>
/// <para>
/// A layered application embeds its documents where the slices are - a domain project referenced by the host that
/// was asked for - so the assemblies the output references, and that sit beside it, are read as well. That is the
/// same set the explorer an application hosts itself serves.
/// </para>
/// </remarks>
public sealed class EmbeddedEventModels : IDisposable
{
    readonly AssemblyLoadContext _context;

    EmbeddedEventModels(AssemblyLoadContext context, IReadOnlyList<IEventModelResources> resources)
    {
        _context = context;
        Resources = resources;
    }

    /// <summary>
    /// Gets the resources of every assembly that embeds a Screenplay catalog.
    /// </summary>
    public IReadOnlyList<IEventModelResources> Resources { get; }

    /// <summary>
    /// Gets a value indicating whether any assembly embeds Screenplay documents.
    /// </summary>
    public bool Any => Resources.Count > 0;

    /// <summary>
    /// Reads the documents embedded in an output assembly and the assemblies beside it that it references.
    /// </summary>
    /// <param name="assemblyPath">The full path of the output assembly.</param>
    /// <returns>The <see cref="EmbeddedEventModels"/>, which owns the loaded assemblies until disposed.</returns>
    public static EmbeddedEventModels From(string assemblyPath)
    {
        var context = new AssemblyLoadContext($"cratis-view:{Path.GetFileName(assemblyPath)}", isCollectible: true);
        var directory = Path.GetDirectoryName(assemblyPath)!;
        var resources = new List<IEventModelResources>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>([assemblyPath]);

        while (pending.TryDequeue(out var path))
        {
            if (!visited.Add(Path.GetFileNameWithoutExtension(path)) || Inspect(path) is not { } inspected)
            {
                continue;
            }

            if (inspected.EmbedsCatalog && Load(context, path) is { } assembly)
            {
                resources.Add(new AssemblyEventModelResources(assembly));
            }

            foreach (var reference in inspected.References)
            {
                var candidate = Path.Combine(directory, $"{reference}.dll");
                if (File.Exists(candidate))
                {
                    pending.Enqueue(candidate);
                }
            }
        }

        return new EmbeddedEventModels(context, resources);
    }

    /// <inheritdoc/>
    public void Dispose() => _context.Unload();

    /// <summary>
    /// Reads whether an assembly embeds a catalog, and what it references, from its metadata alone.
    /// </summary>
    /// <param name="path">The path of the assembly.</param>
    /// <returns>What the metadata says, or <see langword="null"/> when the file is not a managed assembly.</returns>
    /// <remarks>
    /// An application's output holds every package it depends on. Reading their metadata rather than loading them
    /// keeps walking the references cheap, and only an assembly that does embed documents is ever loaded.
    /// </remarks>
    static InspectedAssembly? Inspect(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var portableExecutable = new PEReader(stream);
            if (!portableExecutable.HasMetadata)
            {
                return null;
            }

            var metadata = portableExecutable.GetMetadataReader();
            if (!metadata.IsAssembly)
            {
                return null;
            }

            var embedsCatalog = metadata.ManifestResources.Any(_ =>
                string.Equals(metadata.GetString(metadata.GetManifestResource(_).Name), EventModelCatalog.ResourceName, StringComparison.Ordinal));
            var references = metadata.AssemblyReferences
                .Select(_ => metadata.GetString(metadata.GetAssemblyReference(_).Name))
                .ToList();

            return new InspectedAssembly(embedsCatalog, references);
        }
        catch (BadImageFormatException)
        {
            // A native library that happens to share a name cannot embed documents; it is passed over.
            return null;
        }
    }

    static Assembly? Load(AssemblyLoadContext context, string path)
    {
        try
        {
            using var stream = new MemoryStream(File.ReadAllBytes(path));
            return context.LoadFromStream(stream);
        }
        catch (FileLoadException)
        {
            return null;
        }
    }

    /// <summary>
    /// What the metadata of an assembly says about it.
    /// </summary>
    /// <param name="EmbedsCatalog">Whether the assembly embeds a Screenplay catalog.</param>
    /// <param name="References">The names of the assemblies it references.</param>
    sealed record InspectedAssembly(bool EmbedsCatalog, IReadOnlyList<string> References);
}
