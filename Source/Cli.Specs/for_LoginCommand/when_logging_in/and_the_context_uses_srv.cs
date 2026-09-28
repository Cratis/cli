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

    [Fact] void should_log_in_to_the_direct_server() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_request_a_token_from_the_direct_server() => _endpoint.RequestUri!.Host.ShouldEqual("production");
    [Fact] void should_bind_the_token_to_the_direct_server() => CliConfiguration.Load().Contexts["production"].TokenServer.ShouldEqual("production:35000");
    [Fact] void should_not_send_the_token_to_the_srv_context() => new ChronicleSettings().ResolveConnectionString().ShouldNotContain("apiKey=");
}
