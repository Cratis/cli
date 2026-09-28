// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_server_is_invalid : given.a_login_command
{
    int _result;

    void Establish() => _settings.Server = "chronicle://production:invalid-port";

    async Task Because() => _result = await Execute();

    [Fact] void should_report_an_authentication_error() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_contact_the_server() => _endpoint.RequestUri.ShouldBeNull();
    [Fact] void should_preserve_credentials() => CliConfiguration.Load().Contexts["production"].ClientId.ShouldEqual("old-client");
}
