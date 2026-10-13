// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ArcScreenplayGeneration.when_generating;

public class and_authoring_only_constructs_are_not_requested : given.an_application_with_a_response_only_command
{
    GeneratedScreenplay _result;
    SemanticVersion? _semanticVersion;

    void Because()
    {
        _result = ArcScreenplayGeneration.GenerateFrom(Loaded, $"{ProjectName}.csproj", ScreenplayGenerationOptions.Default);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ProjectName));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("Bookshop.play"), "Bookshop.play", "Bookshop.play", _result.Source);
        var compilation = new SemanticModelCompiler().Compile(ProjectName, SemanticDocumentSet.Create([document], catalog));
        _semanticVersion = compilation.Value?.Model.SemanticVersion;
    }

    [Fact] void should_bind_the_generated_response_as_esm_v7() => _semanticVersion.ShouldEqual(SemanticVersion.V7);
    [Fact] void should_describe_the_response() => _result.Source.ShouldContain("returns bookId");
    [Fact] void should_not_emit_an_authoring_only_handler() => _result.Source.ShouldNotContain("handler");
    [Fact] void should_not_report_generation_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
