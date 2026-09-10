// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Reads source-framework package and assembly provenance without loading framework runtime assemblies.
/// </summary>
static class ScreenplayPackageProvenance
{
    static readonly HashSet<string> _frameworkAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "JasperFx.Events",
        "Marten",
        "Vogen",
        "Wolverine",
        "Wolverine.Marten"
    };

    /// <summary>
    /// Reads the resolved source-framework packages for one selected target from a NuGet assets file.
    /// </summary>
    /// <param name="assetsFile">The assets file written by restore.</param>
    /// <param name="targetFramework">The target framework selected by the workspace.</param>
    /// <returns>The relevant resolved packages in deterministic order.</returns>
    public static IReadOnlyList<ResolvedScreenplayPackage> PackagesFrom(string? assetsFile, string? targetFramework)
    {
        if (string.IsNullOrWhiteSpace(assetsFile) || !File.Exists(assetsFile))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(assetsFile));
            if (!document.RootElement.TryGetProperty("targets", out var targets) ||
                targets.ValueKind != JsonValueKind.Object ||
                TargetOf(targets, targetFramework) is not { } target)
            {
                return [];
            }

            return
            [
                .. target.Value.EnumerateObject()
                    .Where(entry => IsFrameworkPackage(entry.Name) && IsPackage(entry.Value))
                    .Select(entry => PackageFrom(entry.Name))
                    .Where(package => package is not null)
                    .Cast<ResolvedScreenplayPackage>()
                    .OrderBy(package => package.Id, StringComparer.Ordinal)
                    .ThenBy(package => package.Version, StringComparer.Ordinal)
            ];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>
    /// Gets framework assembly identities referenced by a compilation.
    /// </summary>
    /// <param name="compilation">The selected project compilation.</param>
    /// <returns>The relevant identities in deterministic order.</returns>
    public static IReadOnlyList<ScreenplayAssemblyIdentity> AssembliesFrom(Compilation compilation) =>
    [
        .. compilation.ReferencedAssemblyNames
            .Where(identity => _frameworkAssemblies.Contains(identity.Name))
            .Select(identity => new ScreenplayAssemblyIdentity(identity.Name, identity.Version.ToString()))
            .OrderBy(identity => identity.Name, StringComparer.Ordinal)
            .ThenBy(identity => identity.Version, StringComparer.Ordinal)
    ];

    static JsonProperty? TargetOf(JsonElement targets, string? targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            return null;
        }

        var available = targets.EnumerateObject().OrderBy(target => target.Name, StringComparer.Ordinal).ToArray();
        var selected = available
            .Where(target => target.Value.ValueKind == JsonValueKind.Object && string.Equals(target.Name, targetFramework, StringComparison.OrdinalIgnoreCase))
            .Cast<JsonProperty?>()
            .FirstOrDefault();
        return selected ?? available
            .Where(target => target.Value.ValueKind == JsonValueKind.Object && target.Name.StartsWith($"{targetFramework}/", StringComparison.OrdinalIgnoreCase))
            .Cast<JsonProperty?>()
            .FirstOrDefault();
    }

    static bool IsFrameworkPackage(string library) =>
        library.StartsWith("Marten/", StringComparison.OrdinalIgnoreCase) ||
        library.StartsWith("Vogen/", StringComparison.OrdinalIgnoreCase) ||
        library.StartsWith("WolverineFx/", StringComparison.OrdinalIgnoreCase) ||
        library.StartsWith("WolverineFx.", StringComparison.OrdinalIgnoreCase);

    static bool IsPackage(JsonElement library) =>
        library.ValueKind == JsonValueKind.Object &&
        library.TryGetProperty("type", out var type) &&
        type.ValueKind == JsonValueKind.String &&
        string.Equals(type.GetString(), "package", StringComparison.OrdinalIgnoreCase);

    static ResolvedScreenplayPackage? PackageFrom(string library)
    {
        var separator = library.LastIndexOf('/');
        return separator <= 0 || separator == library.Length - 1
            ? null
            : new ResolvedScreenplayPackage(library[..separator], library[(separator + 1)..]);
    }
}
