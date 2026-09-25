// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments;

public class with_invalid_source_and_a_missing_file : given.a_model_root
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => WriteSource(FileSource + "\nnot valid screenplay\n");
    async Task Because() => _result = await _planning.Plan(new(_root, "Orders", "cratis"), CancellationToken.None);

    [Fact] void should_fail_compilation() => _compiled!.Success.ShouldBeFalse();
    [Fact] void should_still_report_the_attachment_warning() => _result.Diagnostics.Any(_ => _.Code == "PLAY0432" && _.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_not_plan_artifacts() => _result.Artifacts.ShouldBeNull();
}
