// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_scopes_are_forwarded : given.a_render_command
{
    int _result;

    void Establish() => _settings.Scope = ["Orders.Placing", "Customers.Registration"];
    async Task Because() => _result = await Execute();

    [Fact] void should_forward_every_scope() => _planning.Received(1).Plan(Arg.Is<ScreenplayRenderRequest>(request => request.Scope.SequenceEqual(_settings.Scope)), Arg.Any<CancellationToken>());
    [Fact] void should_return_success() => _result.ShouldEqual(ExitCodes.Success);
}
