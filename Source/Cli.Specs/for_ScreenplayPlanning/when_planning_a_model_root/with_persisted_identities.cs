// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class with_persisted_identities : given.a_persisted_model_root
{
    ScreenplayWorkspace _workspace = null!;
    SemanticCompilation _temporary = null!;

    void Establish()
    {
        // Fresh MCP persistence retains bootstrap IDs. Assigned IDs model a root whose identities
        // already diverged through authoring/migration, so a temporary-identity fallback cannot pass.
        var path = Path.Combine(_root, ".screenplay", "identities.json");
        var state = PersistedIdentityStateSerializer.Deserialize(File.ReadAllBytes(path));
        var original = state.IdentityCatalog;
        var catalog = SemanticIdentityCatalog.Create(
            original.Application,
            original.Documents,
            [.. original.Semantics.Select(assignment => assignment.Address.Kind is SemanticKind.CompositeType or SemanticKind.EventContract
                ? assignment with { Id = SemanticId.Create(assignment.Address.Kind, $"saved-{assignment.Id}"), Origin = SemanticIdentityOrigin.Persisted }
                : assignment)],
            [.. original.EventContracts.Select(assignment => assignment with { Id = EventContractId.Create(original.Application, $"saved-{assignment.Id}"), Origin = SemanticIdentityOrigin.Persisted })]);
        File.WriteAllBytes(path, PersistedIdentityStateSerializer.Serialize(state with { IdentityCatalog = catalog }));
        _workspace = OpenWorkspace();
        _temporary = new SemanticModelCompiler().Compile("Model", TemporaryRequest().Documents).Value!;
    }

    async Task Because() => _result = await _planning.Plan(new(_root, "Model", "cratis"), CancellationToken.None);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_use_the_persisted_event_contract() => Recorded(_requests.Single().Model.Application).ContractId.ShouldEqual(Recorded(_workspace.Compilation.Value!.Model.Application).ContractId);
    [Fact] void should_not_use_the_temporary_event_contract() => Recorded(_requests.Single().Model.Application).ContractId.ShouldNotEqual(Recorded(_temporary.Model.Application).ContractId);
    [Fact] void should_use_the_persisted_type_identity() => _requests.Single().Model.Application.Types.Single().Id.ShouldEqual(_workspace.Compilation.Value!.Model.Application.Types.Single().Id);
    [Fact] void should_not_use_the_temporary_type_identity() => _requests.Single().Model.Application.Types.Single().Id.ShouldNotEqual(_temporary.Model.Application.Types.Single().Id);
    [Fact] void should_render_the_opened_revision() => _result.Artifacts!.SemanticRevision.ShouldEqual(_workspace.Compilation.Value!.Model.Revision);
    [Fact] void should_attribute_the_event_artifact_to_its_persisted_identity() => _result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Record.cs", StringComparison.Ordinal)).Sources.ShouldContain(Recorded(_workspace.Compilation.Value!.Model.Application).Id);
    [Fact] void should_attribute_the_type_artifact_to_its_persisted_identity() => _result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Details.cs", StringComparison.Ordinal)).Sources.ShouldContain(_workspace.Compilation.Value!.Model.Application.Types.Single().Id);
    [Fact] void should_not_recompile_temporary_documents() => _compiler.DidNotReceive().Compile(Arg.Any<string>(), Arg.Any<SemanticDocumentSet>());
    [Fact] void should_not_warn_without_a_baseline() => _result.Diagnostics.Where(diagnostic => diagnostic.Code.StartsWith("STAGE-BASELINE-", StringComparison.Ordinal)).ShouldBeEmpty();
}
