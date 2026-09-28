// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_saving_the_configuration_fails : given.a_login_command
{
    int _result;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _settings.Server = "chronicle://production:35000";
        var configPath = CliConfiguration.GetConfigPath();
        File.Delete(configPath);
        Directory.CreateDirectory(configPath);
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
    [Fact] void should_report_the_save_failure() => _error.ToString().ShouldContain("Could not save login");
    [Fact] void should_not_persist_the_token() => Directory.Exists(CliConfiguration.GetConfigPath()).ShouldBeTrue();
}
