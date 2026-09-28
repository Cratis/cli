// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_a_server_is_specified : given.a_login_command
{
    int _result;

    void Establish() => _settings.Server = "chronicle://override:35001/?skipTlsValidation=true";

    async Task Because() => _result = await Execute();

    [Fact] void should_not_call_the_override_server() => _endpoint.RequestUri.ShouldBeNull();
    [Fact] void should_not_store_a_token() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldBeNull();
    [Fact] void should_preserve_the_context_credentials() => CliConfiguration.Load().Contexts["production"].ClientId.ShouldEqual("old-client");
    [Fact] void should_refuse_the_login() => _result.ShouldEqual(ExitCodes.AuthenticationError);
}
