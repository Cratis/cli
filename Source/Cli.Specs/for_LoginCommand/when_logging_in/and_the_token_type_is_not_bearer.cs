// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_token_type_is_not_bearer : given.a_login_command
{
    int _result;

    void Establish() => _endpoint.Body = "{\"access_token\":\"user-token\",\"expires_in\":3600,\"token_type\":\"Basic\"}";

    async Task Because() => _result = await Execute();

    [Fact] void should_reject_the_token() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_store_the_token() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldBeNull();
}
