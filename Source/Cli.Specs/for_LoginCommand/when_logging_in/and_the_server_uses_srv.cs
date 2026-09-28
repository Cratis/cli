// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_server_uses_srv : given.a_login_command
{
    int _result;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _settings.Server = "chronicle+srv://production:35000";
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

    [Fact] void should_refuse_to_log_in_via_srv() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_explain_why_login_was_refused() => _error.ToString().ShouldContain("the resolved host cannot be bound to the stored token");
    [Fact] void should_not_request_a_token() => _endpoint.RequestUri.ShouldBeNull();
    [Fact] void should_leave_the_context_untouched() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldBeNull();
}
