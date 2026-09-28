// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_context_uses_srv : given.a_login_command
{
    int _result;

    void Establish()
    {
        var config = CliConfiguration.Load();
        config.Contexts["production"].Server = "chronicle+srv://production:35000";
        config.Save();
        _settings.Server = "chronicle://production:35000";
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_refuse_to_bind_a_token_to_the_srv_context() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_request_a_token() => _endpoint.RequestUri.ShouldBeNull();
    [Fact] void should_leave_the_context_untouched() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldBeNull();
}
