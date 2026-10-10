// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_checking_multiple_refusals : given.a_publication_check
{
    string[] _refusedPaths = [];

    async Task Establish()
    {
        await Publish();
        _refusedPaths = [.. _plan.Artifacts.Take(2).Select(_ => _.RelativePath), "stale.cs"];
        foreach (var path in _refusedPaths.Take(2))
        {
            await File.WriteAllTextAsync(ArtifactPath(path), "modified active bytes");
        }

        AddStale("stale.cs", modified: true);
        _before = Snapshot(_destination);
    }

    async Task Because() => _result = await Check();

    [Fact] void should_report_all_three_refusals() => _result.Refused.ShouldEqual(3);
    [Fact] void should_name_every_refused_path() => _result.Receipt.Changes.Where(_ => _.Kind == "refused").Select(_ => _.Path).ShouldEqual(_refusedPaths);
    [Fact] void should_give_every_refusal_a_reason() => _result.Receipt.Changes.Where(_ => _.Kind == "refused").All(_ => !string.IsNullOrEmpty(_.Reason)).ShouldBeTrue();
    [Fact] void should_report_each_artifact_path_once() => _result.Receipt.Changes.Select(_ => _.Path).Distinct(StringComparer.Ordinal).Count().ShouldEqual(_plan.Artifacts.Length + 1);
    [Fact] void should_leave_every_file_and_directory_unchanged() => Snapshot(_destination).ShouldEqual(_before);
}
