// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_server_redirects_the_request : given.a_login_command
{
    int _result;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _endpoint.Status = HttpStatusCode.Redirect;
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    async Task Because() => _result = await Execute();

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            Console.SetError(_previousError);
        }
        finally
        {
            base.CleanUp();
        }
    }

    [Fact] void should_fail_authentication() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_explain_redirects_are_not_allowed() => _error.ToString().ShouldContain("redirects are not allowed");
    [Fact] void should_not_store_the_token() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldBeNull();
}
