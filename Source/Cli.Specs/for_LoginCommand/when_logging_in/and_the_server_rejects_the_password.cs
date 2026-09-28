// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_server_rejects_the_password : given.a_login_command
{
    int _result;

    void Establish() => _endpoint.Status = HttpStatusCode.Unauthorized;

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_authentication() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_leave_the_context_untouched() => CliConfiguration.Load().Contexts["production"].LoggedInUser.ShouldBeNull();
}
