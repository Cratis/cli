// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_token_response_is_not_an_object : given.a_login_command
{
    int _result;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _endpoint.Body = "[]";
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
    [Fact] void should_report_an_unusable_token() => _error.ToString().ShouldContain("Server did not return a usable access token and expiry.");
    [Fact] void should_not_record_a_login() => CliConfiguration.Load().Contexts["production"].LoggedInUser.ShouldBeNull();
    [Fact] void should_not_clear_existing_credentials() => CliConfiguration.Load().Contexts["production"].ClientId.ShouldEqual("old-client");
}
