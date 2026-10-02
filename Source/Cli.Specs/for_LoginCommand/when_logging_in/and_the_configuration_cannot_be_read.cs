// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_configuration_cannot_be_read : given.a_login_command
{
    int _result;
    FileStream _lockedConfig = null!;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _lockedConfig = new FileStream(CliConfiguration.GetConfigPath(), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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
            _lockedConfig.Dispose();
            Console.SetError(_previousError);
        }
        finally
        {
            base.CleanUp();
        }
    }

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_identify_the_configuration_error() => _error.ToString().ShouldContain("Invalid CLI configuration");
    [Fact] void should_not_call_the_server() => _endpoint.RequestUri.ShouldBeNull();
}
