// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class and_a_compiler_error_has_mapped_source_context : given.a_marten_application_built_from_source
{
    const string PhysicalCompilerPath = "/private/checkout/Banking/Account.cs";
    const string LogicalCompilerPath = "Banking/Account.cs";

    GeneratedScreenplay _result;

    void Because()
    {
        var source = $"{Loaded.Compilations[0].SyntaxTrees.Single().GetText()}\npublic class Broken {{ MissingType Value; }}";
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: PhysicalCompilerPath);
        var compilation = Loaded.Compilations[0].RemoveAllSyntaxTrees().AddSyntaxTrees(syntaxTree);
        var sourceContext = DotNetSourcePaths.Create(
            ProjectName,
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Workspace,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = syntaxTree,
                    ProjectRelativePath = "Account.cs",
                    WorkspaceRelativePath = LogicalCompilerPath
                }
            ]);
        var projectSource = new ScreenplayProjectSource(
            "/private/checkout/Banking/Banking.csproj",
            "Banking/Banking.csproj",
            sourceContext);
        var loaded = Loaded with
        {
            Compilations = [compilation],
            AuthoredSyntaxTrees = [compilation.SyntaxTrees.ToHashSet()],
            ProjectSources = [projectSource]
        };

        _result = CritterStackScreenplayGeneration.GenerateFrom(
            loaded,
            "/private/checkout/Banking/Banking.csproj",
            ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.Marten });
    }

    [Fact] void should_use_the_mapped_display_path() => _result.Diagnostics.Single().Location.ShouldEqual(LogicalCompilerPath);
    [Fact] void should_not_leak_the_roslyn_physical_path() => _result.Diagnostics.Single().Location.ShouldNotContain(PhysicalCompilerPath);
}
