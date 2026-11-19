// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Screenplay.Forward;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_scene_conversion_refuses_ui_syntax : given.a_screenplay_planning
{
    UnsupportedUiSyntax _failure = null!;
    ScreenplayRenderPlan _result = null!;

    ScreenplayDiagnostic Diagnostic => _result.Diagnostics.Single(_ => _.Code == UnsupportedUiSyntax.DiagnosticCode);

    void Establish()
    {
        _failure = new("FutureUiSyntax", new SourceLocation(7, 9, "profile.play"));
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster(), _ => throw _failure);
    }

    async Task Because() => _result = await Plan();

    [Fact] void should_fail_the_plan() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_plan_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_the_conversion_code() => Diagnostic.Code.ShouldEqual(UnsupportedUiSyntax.DiagnosticCode);
    [Fact] void should_report_an_error() => Diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_preserve_the_conversion_message() => Diagnostic.Message.ShouldEqual(_failure.Message);
    [Fact] void should_preserve_the_authored_location() => Diagnostic.Location.ShouldEqual(_failure.Location.ToString());
}
