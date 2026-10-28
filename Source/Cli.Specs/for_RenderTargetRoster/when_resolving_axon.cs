// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderTargetRoster;

public class when_resolving_axon : Specification
{
    IRenderTarget? _target;
    bool _found;

    void Because() => _found = new RenderTargetRoster().TryGet("AxOn", out _target);

    [Fact] void should_resolve_case_insensitively() => _found.ShouldBeTrue();
    [Fact] void should_resolve_the_axon_adapter() => _target.ShouldBeOfExactType<AxonRenderTarget>();
}
