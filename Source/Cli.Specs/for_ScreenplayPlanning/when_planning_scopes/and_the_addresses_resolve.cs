// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

public class and_the_addresses_resolve : given.a_scoped_model
{
    ScreenplayRenderPlan[] _results = null!;

    async Task Because() => _results = [await Plan("Projects"), await Plan("Tasks.Backlog.Intake"), await Plan("Tasks.Backlog.Intake.CaptureTask")];

    [Fact] void should_admit_every_address_kind() => _results.All(result => result.Success).ShouldBeTrue();
    [Fact] void should_resolve_a_module() => _results[0].Artifacts!.Scope.Kind.ShouldEqual(ArtifactRenderScopeKind.Module);
    [Fact] void should_resolve_nested_features() => _results[1].Artifacts!.Scope.Kind.ShouldEqual(ArtifactRenderScopeKind.Feature);
    [Fact] void should_resolve_a_slice() => _results[2].Artifacts!.Scope.Kind.ShouldEqual(ArtifactRenderScopeKind.Slice);
}
