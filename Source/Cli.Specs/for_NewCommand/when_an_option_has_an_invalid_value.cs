// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NewCommand;

[Collection(CliSpecsCollection.Name)]
public class when_an_option_has_an_invalid_value : Specification
{
    StringWriter _output = null!;
    TextWriter _previousOutput = null!;
    int _exitCode;

    void Establish()
    {
        _output = new StringWriter();
        _previousOutput = Console.Out;
        Console.SetOut(_output);
    }

    async Task Because() => _exitCode = await CliApp.Create().RunAsync(
        ["new", "cratis", "--language", "csharp", "--allow-scripts", "maybe", "--format", "json"],
        TestContext.Current.CancellationToken);

    [Fact] void should_exit_with_the_invalid_usage_code() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_explain_the_invalid_value() => _output.ToString().ShouldContain("--allow-scripts must be one of: yes, no, prompt.");

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        _output.Dispose();
    }
}
