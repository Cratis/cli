// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderBootstrap;

public class when_receiving_a_stdin_handoff : given.a_launcher
{
    StudioConnection _connection;

    void Establish()
    {
        _fields[BuilderBootstrap.HandoffVariable] = BuilderBootstrap.StdinVersion;
    }

    async Task Because() => _connection = (await Open())!;

    [Fact] void should_use_the_pipe_credential() => _connection.Token.ShouldEqual("from-pipe");
    [Fact] void should_consume_one_line() => _reads.ShouldEqual(1);
}
