// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_code_has_an_element_missing_from_the_model : given.a_conform_command
{
    void Establish() => Generated(Source + "      event Extra\n        name String\n");
    async Task Because() => await Execute();
    [Fact] void should_report_defects() => _exitCode.ShouldEqual(1);
    [Fact] void should_report_the_code_only_event() => Findings("missingFromModel").Any(finding => finding.GetProperty("address").GetString()!.EndsWith("Extra", StringComparison.Ordinal)).ShouldBeTrue();
}
