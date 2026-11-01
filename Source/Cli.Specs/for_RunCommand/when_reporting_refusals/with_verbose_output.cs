// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_reporting_refusals;

[Collection(CliSpecsCollection.Name)]
public class with_verbose_output : given.a_refusal_output
{
    [Theory]
    [InlineData(OutputFormats.Table)]
    [InlineData(OutputFormats.Plain)]
    void should_include_all_original_issues_after_the_summary(string format)
    {
        RunOutput.WriteRefusal(format, _status, new() { Verbose = true });
        var output = _output.ToString();
        foreach (var issue in _status.Issues!)
        {
            output.ShouldContain($"Unsupported({issue.Capability}) {issue.Artifact}: {issue.Details}");
        }

        (output.IndexOf("Ignored by this engine (5)", StringComparison.Ordinal) < output.IndexOf("Unsupported(Plan)", StringComparison.Ordinal)).ShouldBeTrue();
        if (format == OutputFormats.Table) ErrorPanels(output).ShouldEqual(1);
    }
}
