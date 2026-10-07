// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ArcScreenplayGeneration.when_generating;

public class and_authoring_only_constructs_are_requested : given.an_application_with_a_response_only_command
{
    GeneratedScreenplay _result;
    bool _hasExecutableModel;
    IEnumerable<string> _executableDiagnosticCodes;

    void Because()
    {
        _result = ArcScreenplayGeneration.GenerateFrom(
            Loaded,
            $"{ProjectName}.csproj",
            ScreenplayGenerationOptions.Default with { AuthoringOnlyConstructs = true });
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ProjectName));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("Bookshop.play"), "Bookshop.play", "Bookshop.play", _result.Source);
        var compilation = new SemanticModelCompiler().Compile(ProjectName, SemanticDocumentSet.Create([document], catalog));
        _hasExecutableModel = compilation.Success;
        _executableDiagnosticCodes = compilation.Diagnostics.Select(_ => _.Code);
    }

    [Fact] void should_retain_the_authoring_only_handler() => _result.Source.ShouldContain("handler");
    [Fact] void should_retain_the_implementation_file() => _result.Source.ShouldContain("CheckAvailability.cs");
    [Fact] void should_have_no_executable_model() => _hasExecutableModel.ShouldBeFalse();
    [Fact] void should_report_the_authoring_only_admission_diagnostic() => _executableDiagnosticCodes.ShouldContain("PLAY0268");
    [Fact] void should_not_report_generation_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
