// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.CritterStack.Screenplay;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ProviderScreenplayGeneration.given;

public class an_application_scope : Specification
{
    protected static readonly string MartenSource = string.Join(
        '\n',
        [
            "namespace Marten",
            "{",
            "    public interface IDocumentStore;",
            "    public class StoreOptions",
            "    {",
            "        public Marten.Events.Projections.ProjectionOptions Projections { get; } = new();",
            "    }",
            "}",
            "namespace Marten.Events.Projections",
            "{",
            "    public enum SnapshotLifecycle { Inline }",
            "    public class ProjectionOptions",
            "    {",
            "        public void Snapshot<T>(SnapshotLifecycle lifecycle) { }",
            "    }",
            "}",
            "namespace Banking",
            "{",
            "    public record AccountOpened(System.Guid AccountId);",
            "    public class Account",
            "    {",
            "        public System.Guid Id { get; set; }",
            "        public void Apply(AccountOpened opened) { }",
            "    }",
            "    public static class Configuration",
            "    {",
            "        public static void Configure(Marten.StoreOptions options) =>",
            "            options.Projections.Snapshot<Account>(Marten.Events.Projections.SnapshotLifecycle.Inline);",
            "    }",
            "}"
        ]);

    protected static readonly string WolverineSource = string.Join(
        '\n',
        [
            "namespace Wolverine",
            "{",
            "    public sealed class WolverineOptions;",
            "}"
        ]);

    protected static readonly string VogenConceptSource = string.Join(
        '\n',
        [
            "namespace Concepts",
            "{",
            "    [Vogen.ValueObject<System.Guid>]",
            "    public partial struct OrderId;",
            "}"
        ]);

    protected static readonly string ArcSource = string.Join(
        '\n',
        [
            "namespace Cratis.Arc.Commands.ModelBound",
            "{",
            "    public sealed class CommandAttribute : System.Attribute;",
            "}",
            "namespace Cratis.Chronicle.Events",
            "{",
            "    public sealed class EventTypeAttribute : System.Attribute;",
            "}",
            "namespace Ordering.Placement",
            "{",
            "    [Cratis.Chronicle.Events.EventType]",
            "    public record OrderPlaced(string Customer);",
            string.Empty,
            "    [Cratis.Arc.Commands.ModelBound.Command]",
            "    public record PlaceOrder(string Customer);",
            "}"
        ]);

    protected static readonly ResolvedScreenplayPackage[] MartenPackages =
    [
        new("Marten", "9.20.1")
    ];

    protected static readonly ResolvedScreenplayPackage[] CritterStackPackages =
    [
        new("Marten", "9.23.0"),
        new("WolverineFx", "6.29.1"),
        new("WolverineFx.Marten", "6.29.1")
    ];

    protected static readonly ResolvedScreenplayPackage[] VogenMartenPackages =
    [
        new("Marten", "9.29.0"),
        new("Vogen", "8.0.7")
    ];

    protected static readonly ResolvedScreenplayPackage[] VogenCritterStackPackages =
    [
        new("Marten", "9.29.0"),
        new("Vogen", "8.0.7"),
        new("WolverineFx", "6.29.2")
    ];

    protected static project_source Project(
        string name,
        IReadOnlyList<ResolvedScreenplayPackage> packages,
        bool referencesVogen,
        params string[] sources) => new(name, packages, referencesVogen, sources);

    protected static LoadedCompilation LoadedFrom(params project_source[] projects)
    {
        var compilations = projects.Select(CompilationFrom).ToArray();
        return new LoadedCompilation(
            compilations,
            [.. projects.Select(project => project.Name)],
            [])
        {
            AuthoredSyntaxTrees = [.. compilations.Select(compilation => compilation.SyntaxTrees.ToHashSet())],
            ProjectProvenance =
            [
                .. projects.Select((project, index) => new ScreenplayProjectProvenance(
                    project.Name,
                    "net10.0",
                    project.Packages,
                    ScreenplayPackageProvenance.AssembliesFrom(compilations[index]),
                    ScreenplayFrameworkCapabilities.From(compilations[index])))
            ]
        };
    }

    protected static async Task<GeneratedScreenplay> Generate(
        LoadedCompilation loaded,
        string provider = ScreenplayProviders.Auto,
        string targetPath = "/workspace/Application.slnx")
    {
        var generation = new ProviderScreenplayGeneration(
            ScreenplaySourceProviders.Default,
            (_, _, _) => Task.FromResult(loaded));

        return await generation.Generate(
            targetPath,
            Cratis.Cli.Commands.Screenplay.ScreenplayGenerationOptions.Default with { Provider = provider },
            CancellationToken.None);
    }

    protected static GeneratedScreenplayDefinition GenerateWithCritterStackFacade(LoadedCompilation loaded) =>
        new CritterStackScreenplayGenerator().Generate(
            DotNetProjectsFrom(loaded),
            new CritterStackScreenplayOptions { Domain = "Application" });

    protected static IReadOnlyList<DotNetProjectCompilation> DotNetProjectsFrom(LoadedCompilation loaded) =>
    [
        .. loaded.Compilations.Select((compilation, index) => new DotNetProjectCompilation
        {
            Name = loaded.ProjectNames[index],
            ProjectPath = $"/workspace/{loaded.ProjectNames[index]}/{loaded.ProjectNames[index]}.csproj",
            SourceRoot = $"/workspace/{loaded.ProjectNames[index]}",
            Compilation = compilation,
            AuthoredSyntaxTrees = loaded.AuthoredSyntaxTrees[index]
        })
    ];

    static Compilation CompilationFrom(project_source project)
    {
        var references = TrustedPlatformReferences().ToList();
        if (project.ReferencesVogen)
        {
            references.Add(VogenMetadataReference());
        }

        return CSharpCompilation.Create(
            project.Name,
            [.. project.Sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, path: $"/workspace/{project.Name}/{index}.cs"))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    static MetadataReference VogenMetadataReference()
    {
        var source = string.Join(
            '\n',
            [
                "[assembly: System.Reflection.AssemblyVersion(\"8.0.7.0\")]",
                "namespace Vogen",
                "{",
                "    public sealed class ValueObjectAttribute : System.Attribute",
                "    {",
                "        public ValueObjectAttribute(System.Type underlyingType) { }",
                "    }",
                "    public sealed class ValueObjectAttribute<T> : System.Attribute;",
                "}"
            ]);
        var compilation = CSharpCompilation.Create(
            "Vogen",
            [CSharpSyntaxTree.ParseText(source, path: "/metadata/Vogen.cs")],
            TrustedPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        compilation.Emit(stream).Success.ShouldBeTrue();

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    static IEnumerable<MetadataReference> TrustedPlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

    protected sealed record project_source(
        string Name,
        IReadOnlyList<ResolvedScreenplayPackage> Packages,
        bool ReferencesVogen,
        IReadOnlyList<string> Sources);
}
