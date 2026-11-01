// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ArcScreenplayGeneration.when_recovering_a_rendered_application;

/// <summary>
/// The rendered screens application carries a <c language="csharp">Compile</c> item from inside a restored package
/// (<c language="csharp">Microsoft.NET.Test.Sdk</c>'s entry point), which recovery used to refuse with CLI0017.
/// </summary>
public class with_the_screen_composition_corpus : given.a_rendered_application
{
    protected override string ApplicationName => ScreenCompositionCorpus.V1.ApplicationName;
    Task Because() => RenderAndRecover();

    protected override IEnumerable<CanonicalCorpusDocument> Documents => ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder").Documents;

    [Fact] void should_not_refuse_the_source_paths() => _recovered.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.InvalidSourcePath);
    [Fact] void should_report_no_errors() => _recovered.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_have_declarations_to_compare() => SourceDeclarations.Count.ShouldBeGreaterThan(40);
    [Fact] void should_recover_every_declaration_the_source_declares() => SourceDeclarations.Except(RecoveredDeclarations).ShouldBeEmpty();
    [Fact] void should_recover_nothing_the_source_does_not_declare() => RecoveredDeclarations.Except(SourceDeclarations).ShouldBeEmpty();
}
