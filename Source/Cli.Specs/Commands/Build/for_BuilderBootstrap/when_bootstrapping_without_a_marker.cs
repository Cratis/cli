// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderBootstrap;

public class when_bootstrapping_without_a_marker : given.a_launcher
{
    StudioConnection _connection;

    async Task Because() => _connection = (await Open())!;

    [Fact] void should_use_the_environment_credential() => _connection.Token.ShouldEqual("environment");
    [Fact] void should_not_read_stdin() => _reads.ShouldEqual(0);
}
