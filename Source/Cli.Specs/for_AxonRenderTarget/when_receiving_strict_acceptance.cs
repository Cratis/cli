// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_receiving_strict_acceptance : given.an_axon_target
{
    ArtifactRenderPlan _result = null!;

    void Because() => _result = _target.Plan(_compilation, _execution, null, null, [], [], [], [], _warnings, strict: true);

    [Fact] void should_return_the_renderers_plan() => _result.ShouldEqual(_plan);
    [Fact] void should_pass_strict_acceptance_to_the_renderer() => _input.Strict.ShouldBeTrue();
    [Fact] void should_support_shared_strict_acceptance() => _target.SupportsStrictAcceptance.ShouldBeTrue();
    [Fact] void should_not_warn_about_an_ignored_option() => _warnings.ShouldBeEmpty();
}
