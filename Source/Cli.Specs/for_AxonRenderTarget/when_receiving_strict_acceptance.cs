// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_receiving_strict_acceptance : given.an_axon_target
{
    ArtifactRenderPlan _result = null!;

    void Because() => _result = _target.Plan(_compilation, _execution, null, null, [], [], [], [], _warnings, strict: true);

    [Fact] void should_return_the_renderers_plan() => _result.ShouldEqual(_plan);
    [Fact] void should_not_apply_strict_acceptance_to_the_renderer() => _input.Strict.ShouldBeFalse();
    [Fact] void should_warn_once_about_the_ignored_option() => _warnings.Single(warning => warning.Code == "CLI-RENDER-030").Message.ShouldContain("--strict");
    [Fact] void should_keep_the_diagnostic_as_a_warning() => _warnings.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
}
