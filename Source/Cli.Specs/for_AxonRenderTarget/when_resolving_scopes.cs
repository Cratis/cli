// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_resolving_scopes : given.an_axon_target
{
    RenderSelection _selection = null!;

    void Establish() => _selection = RenderSelection.Resolve(["Projects.Registration.RegisterProject", "Projects.Registration.ProjectLookup"], SemanticAddressIndex.From(_compilation.Model.Application, true), []);

    void Because() => _target.Plan(_compilation, _execution, null, null, [], [], [], [], _warnings, selection: _selection);

    [Fact] void should_pass_the_first_scope() => _input.Scope.ShouldEqual(_selection.Scopes[0]);
    [Fact] void should_pass_remaining_scopes() => _input.AdditionalScopes.SequenceEqual(_selection.Scopes.RemoveAt(0)).ShouldBeTrue();
}
