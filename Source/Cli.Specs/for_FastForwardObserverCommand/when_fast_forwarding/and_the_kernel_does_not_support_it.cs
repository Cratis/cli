// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using FastForwardContract = Cratis.Chronicle.Contracts.Observation.FastForward;

namespace Cratis.Cli.for_FastForwardObserverCommand.when_fast_forwarding;

public class and_the_kernel_does_not_support_it : given.a_fast_forward_observer_command
{
    int _result;

    void Establish() => _observers
        .FastForward(Arg.Any<FastForwardContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
        .Returns<Task<FastForwardResponse>>(_ => throw new RpcException(new Status(StatusCode.Unimplemented, "not there")));

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_validation() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_mention_the_required_version() => (_output.ToString() + _error.ToString()).ShouldContain("19.56.0");
}
