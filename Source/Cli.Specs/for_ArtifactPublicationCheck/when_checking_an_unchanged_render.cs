// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_checking_an_unchanged_render : given.a_publication_check
{
    async Task Establish()
    {
        await Publish();
        _before = Snapshot(_destination);
    }

    async Task Because() => _result = await Check();

    [Fact] void should_report_every_artifact_as_unchanged() => _result.Receipt.Changes.All(_ => _.Kind == "unchanged").ShouldBeTrue();
    [Fact] void should_count_every_artifact() => _result.Unchanged.ShouldEqual(_plan.Artifacts.Length);
    [Fact] void should_report_no_writes() => _result.Written.ShouldEqual(0);
    [Fact] void should_report_no_deletions() => _result.Removed.ShouldEqual(0);
    [Fact] void should_report_no_refusals() => _result.Refused.ShouldEqual(0);
    [Fact] void should_leave_every_file_and_directory_unchanged() => Snapshot(_destination).ShouldEqual(_before);
}
