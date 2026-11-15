// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_checking_a_directory_at_a_planned_path : given.a_publication_check
{
    string _blockedPath;

    async Task Establish()
    {
        await Publish();
        _blockedPath = _plan.Artifacts[0].RelativePath;
        File.Delete(ArtifactPath(_blockedPath));
        Directory.CreateDirectory(ArtifactPath(_blockedPath));
        AddStale("stale.cs", modified: true);
        _before = Snapshot(_destination);
    }

    async Task Because() => _result = await Check(force: true);

    [Fact] void should_refuse_both_paths_even_with_force() => _result.Refused.ShouldEqual(2);
    [Fact] void should_report_the_active_path_before_the_stale_path() => _result.Receipt.Changes.Where(_ => _.Kind == "refused").Select(_ => _.Path).ShouldEqual([_blockedPath, "stale.cs"]);
    [Fact] void should_keep_the_stale_refusal_message() => _result.Receipt.Changes.Single(_ => _.Path == "stale.cs").Reason.ShouldEqual("Stale managed artifact 'stale.cs' was modified and will not be removed.");
    [Fact] void should_leave_the_destination_unchanged() => Snapshot(_destination).ShouldEqual(_before);
}
