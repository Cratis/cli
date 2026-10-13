// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_the_comparison_is_incomplete : given.a_conform_command
{
    void Establish() => Generated("invalid top level text\n" + Source);
    async Task Because() => await Execute();
    [Fact] void should_report_an_incomplete_check() => _exitCode.ShouldEqual(2);
    [Fact] void should_report_the_source_coverage_gap() => Findings("gaps").Any(gap => gap.GetProperty("kind").GetString() == "IncompleteSource").ShouldBeTrue();
    [Fact] void should_not_claim_clean() => _output.GetProperty("verdict").GetString().ShouldEqual("incomplete");
}
