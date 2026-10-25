// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_reaction_refusal_branches : given.a_screenplay_planning
{
    static readonly string RefusalSource = string.Join('\n',
        "module Work",
        "  feature Processing",
        "    slice Automation Handling",
        "      event Started",
        "        id String",
        "      event Completed",
        "      event Recorded",
        "        reason String",
        "        constraint String",
        "        message String",
        "      command Start",
        "        id String identifier",
        "        produces Started",
        "          for id",
        "          id = id",
        "      command Complete",
        "        id String identifier",
        "        produces Completed",
        "          for id",
        "      constraint OneCompletion",
        "        unique event Completed",
        "      reaction Worker",
        "        runs as system role \"Automation\"",
        "        when Started",
        "          invokes Complete",
        "            id = id",
        "            on refused by constraint OneCompletion",
        "              produces Recorded",
        "                reason = $refusal.reason",
        "                constraint = $refusal.constraint",
        "                message = $refusal.message",
        "            on refused",
        "              acknowledge");

    ScreenplayRenderPlan _result = null!;
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        File.WriteAllText(_file, RefusalSource);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("refusals"), "refusals", "Refusals.play", RefusalSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        _model = compilation.Value!.Model;
    }

    async Task Because() => _result = await Plan();

    [Fact] void should_compile_to_v19() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V19);
    [Fact] void should_admit_v19() => RenderedSemanticVersions.Check(_model.SemanticVersion).ShouldBeNull();
    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_report_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_render_the_named_constraint_refusal_branch() => ReactionCode.ShouldContain(".OnRefused(Refused.ByConstraint(\"OneCompletion\")");
    [Fact] void should_render_the_fallback_acknowledgement() => ReactionCode.ShouldContain(".Acknowledge(Refused.Any)");
    [Fact] void should_preserve_the_refusal_reason() => ReactionCode.ShouldContain("CommandRefusalKind.Constraint => \"constraint\"");
    [Fact] void should_preserve_the_refusal_constraint() => ReactionCode.ShouldContain("refusal.ConstraintName");
    [Fact] void should_preserve_the_refusal_message() => ReactionCode.ShouldContain("refusal.Message");

    string ReactionCode => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Worker.cs", StringComparison.Ordinal)).Bytes.AsSpan());
}
