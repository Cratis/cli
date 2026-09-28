// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ChronicleCommand.when_connecting;

[Collection(CliSpecsCollection.Name)]
public class and_the_config_is_corrupt : a_temp_config_directory
{
    int _result;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CliConfiguration.GetConfigPath())!);
        File.WriteAllText(CliConfiguration.GetConfigPath(), "{invalid json");
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    async Task Because()
    {
        _result = await ((ICommand<ChronicleSettings>)new TestCommand()).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "test", null),
            new ChronicleSettings { Server = "chronicle://production:35000", Output = OutputFormats.JsonCompact },
            CancellationToken.None);
    }

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

    [Fact] void should_return_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_corrupt_config_in_json() => _error.ToString().ShouldContain("Invalid CLI configuration");

    sealed class TestCommand : ChronicleCommand<ChronicleSettings>
    {
        protected override Task<int> ExecuteCommandAsync(IServices services, ChronicleSettings settings, string format) =>
            Task.FromResult(ExitCodes.Success);
    }
}
