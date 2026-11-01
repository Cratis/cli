// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_forwarding_a_notification : given.a_bridge
{
    void Establish() => _direct.Answer(() => Status(HttpStatusCode.Accepted));

    Task Because() => Forward(Initialized);

    [Fact] void should_forward_it() => _direct.Requests.Single().Body.ShouldEqual(Initialized);
    [Fact] void should_write_nothing_back() => _output.ToString().ShouldBeEmpty();
}
