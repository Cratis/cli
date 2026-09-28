// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_no_token_is_returned : given.a_login_command
{
    int _result;

    void Establish() => _endpoint.Body = "{\"expires_in\":3600}";

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_authentication() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_record_a_login() => CliConfiguration.Load().Contexts["production"].LoggedInUser.ShouldBeNull();
    [Fact] void should_not_clear_existing_credentials() => CliConfiguration.Load().Contexts["production"].ClientId.ShouldEqual("old-client");
}
