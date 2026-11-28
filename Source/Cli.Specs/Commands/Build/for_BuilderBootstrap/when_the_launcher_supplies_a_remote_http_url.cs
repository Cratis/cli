// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderBootstrap;

public class when_the_launcher_supplies_a_remote_http_url : given.a_launcher
{
    Exception _error;

    void Establish()
    {
        _fields[StudioConnection.UrlVariable] = "http://studio.example";
    }

    async Task Because() => _error = await Catch.Exception(Open);

    [Fact] void should_refuse_the_connection() => _error.ShouldBeOfExactType<StudioNotConnected>();
}
