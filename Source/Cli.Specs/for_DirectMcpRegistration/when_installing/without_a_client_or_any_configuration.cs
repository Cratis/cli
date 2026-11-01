// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class without_a_client_or_any_configuration : given.a_home_and_a_project
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => Install(DirectMcpScope.User, []));

    [Fact] void should_ask_for_a_client() => _error.Message.ShouldContain("--client");
    [Fact] void should_write_nothing() => Directory.GetFileSystemEntries(_home).ShouldBeEmpty();
}
