// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_an_event_shape_differs : given.a_conform_command
{
    void Establish() => Generated(Source + "        extra String\n");
    async Task Because() => await Execute();
    [Fact] void should_report_defects() => _exitCode.ShouldEqual(1);
    [Fact] void should_report_the_typed_property_addition() => Findings("shapeMismatches").Any(finding => finding.GetProperty("change").GetString() == "PropertyAdded" && finding.GetProperty("member").GetString() == "extra" && finding.GetProperty("afterType").GetString()!.Contains("String", StringComparison.Ordinal)).ShouldBeTrue();
}
