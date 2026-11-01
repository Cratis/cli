// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ArcScreenplayGeneration.when_recovering_a_rendered_application;

/// <summary>
/// The RegisterProject vector Stage renders. Its v2 and v7 forms are refused by the renderer (an evolved event and
/// generated values), so the legacy v1 form is the one with a rendered application to recover.
/// </summary>
public class with_the_register_project_vector : given.a_rendered_application
{
    protected override string ApplicationName => RegisterProjectCorpus.LegacyV1.ApplicationName;
    Task Because() => RenderAndRecover();

    protected override IEnumerable<CanonicalCorpusDocument> Documents => RegisterProjectCorpus.LegacyV1.SourceForms.Single(_ => _.Name == "folder").Documents;

    [Fact] void should_not_refuse_the_source_paths() => _recovered.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.InvalidSourcePath);
    [Fact] void should_report_no_errors() => _recovered.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_have_declarations_to_compare() => SourceDeclarations.Count.ShouldBeGreaterThan(10);
    [Fact] void should_recover_every_declaration_the_source_declares() => SourceDeclarations.Except(RecoveredDeclarations).ShouldBeEmpty();
    [Fact] void should_recover_nothing_the_source_does_not_declare() => RecoveredDeclarations.Except(SourceDeclarations).ShouldBeEmpty();
}
