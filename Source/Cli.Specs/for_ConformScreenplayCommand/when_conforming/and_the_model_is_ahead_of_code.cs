// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_the_model_is_ahead_of_code : given.a_conform_command
{
    void Establish() => File.WriteAllText(_model, Source + "      screen History\n");
    async Task Because() => await Execute();
    [Fact] void should_not_block_unrealized_model_elements() => _exitCode.ShouldEqual(0);
    [Fact] void should_list_the_unrealized_screen() => Findings("notRealizedInCode").Any(finding => finding.GetProperty("kind").GetString() == "Screen").ShouldBeTrue();
    [Fact] void should_count_informational_findings() => (_output.GetProperty("informationalCount").GetInt32() > 0).ShouldBeTrue();
}
