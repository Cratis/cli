// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_code_matches_the_model : given.a_conform_command
{
    async Task Because() => await Execute();
    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_have_no_blocking_findings() => _output.GetProperty("blockingCount").GetInt32().ShouldEqual(0);
    [Fact] void should_use_address_matching() => _output.GetProperty("matching").GetString().ShouldEqual("Address");
    [Fact] void should_report_expected_coverage_limits() => Findings("gaps").Select(gap => gap.GetProperty("kind").GetString()).ShouldContain("IdentitiesNotCompared");
}
