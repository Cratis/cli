// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_reporting_refusals;

[Collection(CliSpecsCollection.Name)]
public class with_quiet_output : given.a_refusal_output
{
    [Theory]
    [InlineData(OutputFormats.Quiet)]
    [InlineData(OutputFormats.JsonQuiet)]
    void should_keep_quiet_error_reporting_without_a_grouped_summary(string format)
    {
        RunOutput.WriteRefusal(format, _status);
        _output.ToString().ShouldEqual(string.Empty);
        _error.ToString().ShouldContain("Unsupported(Plan)");
        _error.ToString().ShouldContain("The model cannot run on the semantic engine");
        _error.ToString().ShouldNotContain("Ignored by this engine");
    }
}
