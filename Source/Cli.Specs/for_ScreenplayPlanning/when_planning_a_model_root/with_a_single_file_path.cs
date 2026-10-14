// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class with_a_single_file_path : given.a_persisted_model_root
{
    async Task Because() => _result = await _planning.Plan(new(_document, "Model", "cratis"), CancellationToken.None);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_use_its_parent_roots_persisted_revision() => _result.Artifacts!.SemanticRevision.ShouldEqual(OpenWorkspace().Compilation.Value!.Model.Revision);
    [Fact] void should_use_the_persisted_event_contract() => Recorded(_requests.Single().Model.Application).ContractId.ShouldEqual(Recorded(OpenWorkspace().Compilation.Value!.Model.Application).ContractId);
}
