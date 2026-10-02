// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_initializing : given.a_bridge
{
    void Establish()
    {
        _direct.Answer(() => Json("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"protocolVersion\":\"2025-06-18\"}}"));
        _direct.Answer(() => Json("{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}"));
    }

    /// <summary>Forwards sequentially, as a client waits for the initialize response before it sends anything else.</summary>
    /// <returns>A task.</returns>
    async Task Because()
    {
        await Forward(Initialize);
        await Forward(ListTools);
    }

    [Fact] void should_send_the_negotiated_protocol_version_afterwards() => _direct.Requests[1].ProtocolVersion.ShouldEqual("2025-06-18");
    [Fact] void should_not_send_a_protocol_version_before_one_is_negotiated() => _direct.Requests[0].ProtocolVersion.ShouldBeNull();
}
