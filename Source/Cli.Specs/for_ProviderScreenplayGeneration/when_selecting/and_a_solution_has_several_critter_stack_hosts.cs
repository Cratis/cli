// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_selecting;

public class and_a_solution_has_several_critter_stack_hosts : Specification
{
    GeneratedScreenplay? _result;

    void Because()
    {
        var loaded = new LoadedCompilation(
            [Host("Api"), Host("Worker")],
            ["Api", "Worker"],
            []);
        _result = new ProviderScreenplayGeneration().AmbiguousHosts(
            loaded,
            "/workspace/Applications.slnx",
            new CritterStackSourceProvider());
    }

    [Fact] void should_report_an_outcome() => _result.ShouldNotBeNull();
    [Fact] void should_report_both_hosts() => _result!.Diagnostics.Single().Message.ShouldContain("Api, Worker");
    [Fact] void should_report_the_ambiguity_code() => _result!.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.AmbiguousApplicationHosts);
    [Fact] void should_generate_no_source() => _result!.Source.ShouldBeEmpty();

    static CSharpCompilation Host(string name) => CSharpCompilation.Create(
        name,
        [CSharpSyntaxTree.ParseText("public static class Program { public static void Main() { } }")],
        References(),
        new CSharpCompilationOptions(OutputKind.ConsoleApplication));

    static IEnumerable<MetadataReference> References() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(_ => MetadataReference.CreateFromFile(_));
}
