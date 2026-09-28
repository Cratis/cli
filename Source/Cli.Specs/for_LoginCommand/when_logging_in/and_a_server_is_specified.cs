// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_a_server_is_specified : given.a_login_command
{
    int _result;

    void Establish() => _settings.Server = "chronicle://override:35001/?skipTlsValidation=true";

    async Task Because() => _result = await Execute();

    [Fact] void should_call_the_override_server() => _endpoint.RequestUri!.Authority.ShouldEqual("override:35001");
    [Fact] void should_store_the_token_on_the_active_context() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldEqual("user-token");
    [Fact] void should_reuse_the_token_with_the_same_server_override() => new ChronicleSettings { Server = _settings.Server }.ResolveConnectionString().ShouldContain("apiKey=user-token");
    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
}
