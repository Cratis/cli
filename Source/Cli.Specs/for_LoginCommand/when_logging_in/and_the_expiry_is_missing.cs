// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_expiry_is_missing : given.a_login_command
{
    int _result;

    void Establish() => _endpoint.Body = "{\"access_token\":\"user-token\"}";

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_authentication() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_persist_an_unusable_token() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldBeNull();
}
