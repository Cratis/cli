// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderBootstrap;

public class when_the_handoff_protocol_is_unknown : given.a_launcher
{
    Exception _error;

    void Establish() => _fields[BuilderBootstrap.HandoffVariable] = "future-version";
    async Task Because() => _error = await Catch.Exception(Open);

    [Fact] void should_refuse_even_with_complete_environment_credentials() => _error.ShouldBeOfExactType<StudioNotConnected>();
    [Fact] void should_not_consume_any_input() => _reads.ShouldEqual(0);
}
