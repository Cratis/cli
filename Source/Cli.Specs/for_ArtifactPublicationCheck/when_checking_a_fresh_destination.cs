// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_checking_a_fresh_destination : given.a_publication_check
{
    void Establish() => _before = Snapshot(_destination);
    async Task Because() => _result = await Check();

    [Fact] void should_report_all_planned_writes() => _result.Written.ShouldEqual(_plan.Artifacts.Length);
    [Fact] void should_report_each_path_once() => _result.Receipt.Changes.Select(_ => _.Path).ShouldEqual(_plan.Artifacts.Select(_ => _.RelativePath));
    [Fact] void should_leave_every_file_and_directory_unchanged() => Snapshot(_destination).ShouldEqual(_before);
    [Fact] void should_not_create_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
    [Fact] void should_not_create_control_state() => Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
    [Fact] void should_not_create_a_manifest() => File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeFalse();
    [Fact] void should_report_no_refusals() => _result.Refused.ShouldEqual(0);
    [Fact] void should_identify_a_check_not_a_publication() => _result.Receipt.Status.ShouldEqual("check");
}
