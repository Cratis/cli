// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

[Collection(CliSpecsCollection.Name)]
public class with_relocated_provenance_and_diagnostics : given.an_application_scope
{
    const string FirstPhysicalRoot = "/physical/first";
    const string RelocatedPhysicalRoot = "/physical/relocated";

    readonly Dictionary<string, relocated_output> _outputs = [];

    async Task Because()
    {
        await Compare(
            "valid",
            root => MartenAt(root, compilerError: false),
            _ => ScreenplayGenerationOptions.Default with
            {
                Provider = ScreenplayProviders.Marten,
                FeatureRoot = @"Features\.//",
                Module = "Banking"
            });
        await Compare(
            "invalid",
            root => MartenAt(root, compilerError: false),
            root => ScreenplayGenerationOptions.Default with
            {
                Provider = ScreenplayProviders.Marten,
                FeatureRoot = $"{root}/Features"
            });
        await Compare(
            "unsupported",
            ArcAt,
            root => ScreenplayGenerationOptions.Default with
            {
                Provider = ScreenplayProviders.Arc,
                FeatureRoot = $"{root}/Features"
            });
        await Compare(
            "compiler",
            root => MartenAt(root, compilerError: true),
            _ => ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.Marten });
    }

    [Fact] void should_serialize_identical_valid_outputs_after_relocation() => ShouldBeRelocationIdentical("valid");
    [Fact] void should_serialize_identical_invalid_outputs_after_relocation() => ShouldBeRelocationIdentical("invalid");
    [Fact] void should_serialize_identical_unsupported_outputs_after_relocation() => ShouldBeRelocationIdentical("unsupported");
    [Fact] void should_serialize_identical_compiler_outputs_after_relocation() => ShouldBeRelocationIdentical("compiler");
    [Fact] void should_store_only_the_normalized_valid_feature_root() => _outputs["valid"].First.Provenance.Projects.Single().SourceStructure.FeatureRoot.ShouldEqual("Features");
    [Fact] void should_block_an_invalid_feature_root_without_source_or_provenance() => InvalidIsBlocked().ShouldBeTrue();
    [Fact] void should_keep_an_unsupported_feature_root_unapplied() => UnsupportedIsUnapplied().ShouldBeTrue();
    [Fact] void should_map_compiler_errors_to_the_logical_display_path() => _outputs["compiler"].First.Diagnostics.Single().Location.ShouldEqual("Source/Persistence/Features/Lending/Accounts/Account.cs");
    [Fact] void should_not_serialize_physical_roots_or_raw_absolute_options() => AllSerializedOutput().ShouldNotContain("/physical/");

    async Task Compare(
        string name,
        Func<string, LoadedCompilation> loadedAt,
        Func<string, ScreenplayGenerationOptions> optionsAt)
    {
        var first = await GenerateAt(FirstPhysicalRoot, loadedAt(FirstPhysicalRoot), optionsAt(FirstPhysicalRoot));
        var relocated = await GenerateAt(RelocatedPhysicalRoot, loadedAt(RelocatedPhysicalRoot), optionsAt(RelocatedPhysicalRoot));
        _outputs[name] = new(first, relocated, Render(first), Render(relocated));
    }

    static async Task<GeneratedScreenplay> GenerateAt(
        string physicalRoot,
        LoadedCompilation loaded,
        ScreenplayGenerationOptions options)
    {
        var generation = new ProviderScreenplayGeneration(
            ScreenplaySourceProviders.Default,
            (_, _, _) => Task.FromResult(loaded));
        return await generation.Generate(
            $"{physicalRoot}/Application.slnx",
            options,
            CancellationToken.None);
    }

    static serialized_output Render(GeneratedScreenplay result)
    {
        var previousError = Console.Error;
        using var error = new StringWriter();
        try
        {
            Console.SetError(error);
            ScreenplayDiagnosticsWriter.Write(OutputFormats.Plain, result.Diagnostics, result.Provenance);
            var text = error.ToString();
            error.GetStringBuilder().Clear();
            ScreenplayDiagnosticsWriter.Write(OutputFormats.JsonCompact, result.Diagnostics, result.Provenance);
            return new(text, error.ToString());
        }
        finally
        {
            Console.SetError(previousError);
        }
    }

    static LoadedCompilation MartenAt(string physicalRoot, bool compilerError)
    {
        var loaded = LoadedFrom(Project("Persistence", MartenPackages, false, MartenSource));
        return Relocate(
            loaded,
            physicalRoot,
            "Persistence",
            "Features/Lending/Accounts/Account.cs",
            "Source/Persistence/Features/Lending/Accounts/Account.cs",
            compilerError);
    }

    static LoadedCompilation ArcAt(string physicalRoot)
    {
        var loaded = LoadedFrom(Project("Application", [], false, ArcSource));
        return Relocate(
            loaded,
            physicalRoot,
            "Application",
            "Features/Ordering/Placement.cs",
            "Source/Application/Features/Ordering/Placement.cs",
            compilerError: false);
    }

    static LoadedCompilation Relocate(
        LoadedCompilation loaded,
        string physicalRoot,
        string projectName,
        string projectRelativePath,
        string workspaceRelativePath,
        bool compilerError)
    {
        var originalTree = loaded.Compilations.Single().SyntaxTrees.Single();
        var source = originalTree.GetText().ToString();
        if (compilerError)
        {
            source = $"{source}\npublic class Broken {{ MissingType Value; }}";
        }

        var physicalSourcePath = Path.Combine(physicalRoot, workspaceRelativePath);
        var relocatedTree = CSharpSyntaxTree.ParseText(source, path: physicalSourcePath);
        var compilation = loaded.Compilations.Single().ReplaceSyntaxTree(originalTree, relocatedTree);
        var sourceContext = DotNetSourcePaths.Create(
            $"Source/{projectName}/{projectName}",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Workspace,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = relocatedTree,
                    ProjectRelativePath = projectRelativePath,
                    WorkspaceRelativePath = workspaceRelativePath
                }
            ]);
        return loaded with
        {
            Compilations = [compilation],
            AuthoredSyntaxTrees = [compilation.SyntaxTrees.ToHashSet()],
            ProjectSources =
            [
                new ScreenplayProjectSource(
                    Path.Combine(physicalRoot, "Source", projectName, $"{projectName}.csproj"),
                    $"Source/{projectName}/{projectName}.csproj",
                    sourceContext)
                {
                    Role = DotNetProjectRole.Application,
                    SourceRoot = physicalRoot
                }
            ]
        };
    }

    void ShouldBeRelocationIdentical(string name)
    {
        var output = _outputs[name];
        output.Relocated.Source.ShouldEqual(output.First.Source);
        output.Relocated.Diagnostics.SequenceEqual(output.First.Diagnostics).ShouldBeTrue();
        output.RelocatedSerialized.Text.ShouldEqual(output.FirstSerialized.Text);
        output.RelocatedSerialized.Json.ShouldEqual(output.FirstSerialized.Json);
    }

    bool InvalidIsBlocked()
    {
        var result = _outputs["invalid"].First;
        return result.Source.Length == 0 &&
               result.Provenance is null &&
               result.Diagnostics.Count == 1 &&
               result.Diagnostics[0].Severity == ScreenplayDiagnosticSeverity.Error &&
               string.Equals(result.Diagnostics[0].Code, "DOTNETSP0002", StringComparison.Ordinal) &&
               string.Equals(result.Diagnostics[0].Message, "The project-relative feature root is invalid", StringComparison.Ordinal);
    }

    bool UnsupportedIsUnapplied()
    {
        var result = _outputs["unsupported"].First;
        return result.Diagnostics.Any(_ => string.Equals(_.Code, ScreenplayDiagnosticCodes.UnsupportedGenerationOption, StringComparison.Ordinal)) &&
               result.Provenance?.Projects.All(_ => _.SourceStructure?.FeatureRoot is null) == true;
    }

    string AllSerializedOutput() => string.Join(
        '\n',
        _outputs.Values.SelectMany(output => new[]
        {
            output.First.Source,
            output.Relocated.Source,
            output.FirstSerialized.Text,
            output.FirstSerialized.Json,
            output.RelocatedSerialized.Text,
            output.RelocatedSerialized.Json
        }));

    sealed record serialized_output(string Text, string Json);

    sealed record relocated_output(
        GeneratedScreenplay First,
        GeneratedScreenplay Relocated,
        serialized_output FirstSerialized,
        serialized_output RelocatedSerialized);
}
