// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_context_has_multiple_hosts : given.a_login_command
{
    int _result;

    void Establish()
    {
        var config = CliConfiguration.Load();
        config.Contexts["production"].Server = "chronicle://production:35000,backup:35000";
        config.Save();
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_refuse_to_bind_a_token_to_more_than_one_host() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_request_a_token() => _endpoint.RequestUri.ShouldBeNull();
    [Fact] void should_leave_the_context_untouched() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldBeNull();
}
