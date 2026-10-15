// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageRuntimeStatus;

public class when_selecting_a_fallback : Specification
{
    readonly StageRuntimeStatus _status = new("unsupported", "eventmodel", [
        new("Routing", "Register", "Routes run only on the semantic engine."),
        new("Reaction", "Notify", "Use the semantic path to preserve runsAs.")
    ]);

    [Fact] void should_switch_for_an_implicit_eventmodel_engine() => _status.ShouldSwitchToSemantic(null).ShouldBeTrue();
    [Fact] void should_not_switch_an_explicit_eventmodel_engine() => _status.ShouldSwitchToSemantic("eventmodel").ShouldBeFalse();
    [Fact] void should_not_switch_an_explicit_semantic_engine() => _status.ShouldSwitchToSemantic("semantic").ShouldBeFalse();
    [Fact] void should_not_switch_mixed_refusals() => (_status with { Issues = [.. _status.Issues!, new("World", "model", "History cannot be reconstructed.")] }).ShouldSwitchToSemantic(null).ShouldBeFalse();
    [Fact] void should_not_switch_an_already_semantic_engine() => (_status with { Engine = "semantic" }).ShouldSwitchToSemantic(null).ShouldBeFalse();
    [Fact] void should_not_switch_an_unreported_engine() => (_status with { Engine = null }).ShouldSwitchToSemantic(null).ShouldBeFalse();
    [Fact] void should_not_switch_an_empty_refusal_set() => (_status with { Issues = [] }).ShouldSwitchToSemantic(null).ShouldBeFalse();
    [Fact] void should_not_switch_an_absent_refusal_set() => (_status with { Issues = null }).ShouldSwitchToSemantic(null).ShouldBeFalse();
    [Fact] void should_not_switch_a_ready_model() => (_status with { State = "ready" }).ShouldSwitchToSemantic(null).ShouldBeFalse();
}
