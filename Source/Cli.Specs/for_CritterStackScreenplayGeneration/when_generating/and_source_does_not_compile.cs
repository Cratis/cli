// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class and_source_does_not_compile : given.a_marten_application_built_from_source
{
    const string PhysicalCompilerPath = "/private/checkout/Generated/Broken.cs";

    GeneratedScreenplay _result = null!;

    void Because()
    {
        var broken = Loaded.Compilations[0].AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            "public class Broken { MissingType Value; }",
            path: PhysicalCompilerPath));
        var loaded = Loaded with
        {
            Compilations = [broken],
            ProjectSources = [ProjectSource]
        };
        _result = CritterStackScreenplayGeneration.GenerateFrom(
            loaded,
            "/workspace/Banking/Banking.csproj",
            ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.Marten });
    }

    [Fact] void should_generate_no_source() => _result.Source.ShouldBeEmpty();
    [Fact] void should_report_the_compilation_error() => _result.Diagnostics.Select(_ => _.Code).ShouldContainOnly(ScreenplayDiagnosticCodes.SourceDidNotCompile);
    [Fact] void should_report_only_the_stable_compiler_error_identity() => _result.Diagnostics.Single().Message.ShouldEqual($"Source project '{ProjectName}' did not compile: CS0246");
    [Fact] void should_use_the_logical_project_identity_for_an_unmapped_tree() => _result.Diagnostics.Single().Location.ShouldEqual(ProjectName);
    [Fact] void should_not_leak_the_roslyn_physical_path() => _result.Diagnostics.Single().Location.ShouldNotContain(PhysicalCompilerPath);
}
