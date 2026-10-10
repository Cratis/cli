// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_a_model_split_across_subfolders_matches : given.a_split_model
{
    async Task Because() => await Execute();
    [Fact] void should_not_report_a_failed_check() => _error.ShouldNotContain("CLI-CONFORM-001");
    [Fact] void should_conform_without_stable_key_refusals() => _exitCode.ShouldEqual(0);
    [Fact] void should_report_clean() => _output.GetProperty("verdict").GetString().ShouldEqual("clean");
    [Fact] void should_still_match_by_address() => _output.GetProperty("matching").GetString().ShouldEqual("Address");
    [Fact] void should_compare_every_split_declaration() => _output.GetProperty("comparedCounts").GetProperty("model").GetInt32().ShouldEqual(_output.GetProperty("comparedCounts").GetProperty("code").GetInt32());
    [Fact] void should_have_no_findings() => _output.GetProperty("blockingCount").GetInt32().ShouldEqual(0);
}
