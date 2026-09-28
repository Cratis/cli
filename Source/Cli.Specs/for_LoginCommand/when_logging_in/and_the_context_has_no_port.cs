// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_context_has_no_port : given.a_login_command
{
    int _result;

    void Establish()
    {
        var config = CliConfiguration.Load();
        config.Contexts["production"].Server = "chronicle://production";
        config.Save();
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_use_the_chronicle_default_port() => _endpoint.RequestUri!.Port.ShouldEqual(35000);
    [Fact] void should_bind_the_default_port() => CliConfiguration.Load().Contexts["production"].TokenServer.ShouldEqual("production:35000");
    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
}
