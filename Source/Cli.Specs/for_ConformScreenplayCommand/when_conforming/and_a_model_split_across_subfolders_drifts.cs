// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_a_model_split_across_subfolders_drifts : given.a_split_model
{
    void Establish()
    {
        _settings.ModelRoot = _folder;
        Generated(Source + "        extra String\n" + ViewSource);
    }
    async Task Because() => await Execute();
    [Fact] void should_report_a_defect_rather_than_fail_to_compare() => _exitCode.ShouldEqual(1);
    [Fact] void should_report_defects_found() => _output.GetProperty("verdict").GetString().ShouldEqual("defects-found");
    [Fact] void should_retain_the_typed_event_property_drift() => Findings("shapeMismatches").Any(finding => finding.GetProperty("kind").GetString() == "Event" && finding.GetProperty("change").GetString() == "PropertyAdded" && finding.GetProperty("member").GetString() == "extra" && finding.GetProperty("blocking").GetBoolean()).ShouldBeTrue();
}
