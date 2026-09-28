// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_a_different_server_is_specified_for_table_output : given.a_login_command
{
    int _result;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _settings.Server = "chronicle://override:35001";
        _settings.Output = OutputFormats.Table;
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

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_explain_the_token_binding_on_stderr() => _error.ToString().ShouldContain("token will only be used for override:35001");
}
