// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LogoutCommand;

[Collection(CliSpecsCollection.Name)]
public class when_the_token_directory_does_not_exist : given.a_logout_command
{
    Task Because() => Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_clear_the_credentials() => _cleared.ShouldBeTrue();
}
