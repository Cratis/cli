// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Axon;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_the_renderer_is_canceled : given.an_axon_target
{
    void Establish() => _rendering.Plan(Arg.Any<AxonRenderInput>()).Returns(_ => throw new OperationCanceledException());

    [Fact]
    void should_preserve_cancellation() => Assert.Throws<OperationCanceledException>(() =>
        _target.Plan(_compilation, _execution, null, null, [], [], [], [], _warnings));
}
