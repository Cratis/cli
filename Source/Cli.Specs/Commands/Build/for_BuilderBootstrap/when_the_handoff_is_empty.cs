// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderBootstrap;

public class when_the_handoff_is_empty : given.a_launcher
{
    Exception _error;

    void Establish()
    {
        _fields[BuilderBootstrap.HandoffVariable] = BuilderBootstrap.StdinVersion;
        _line = null;
    }

    async Task Because() => _error = await Catch.Exception(Open);

    [Fact] void should_not_fall_back_to_environment_or_saved_credentials() => _error.ShouldBeOfExactType<StudioNotConnected>();
}
