// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_forcing_a_modified_managed_file : given.a_publication_check
{
    string _relativePath = null!;

    async Task Establish()
    {
        await Publish();
        _relativePath = _plan.Artifacts[0].RelativePath;
        await File.WriteAllTextAsync(ArtifactPath(_relativePath), "user modified bytes");
        _before = Snapshot(_destination);
    }

    async Task Because() => _result = await Check(force: true);

    [Fact] void should_report_one_write() => _result.Written.ShouldEqual(1);
    [Fact] void should_name_the_modified_file() => _result.Receipt.Changes.Single(_ => _.Kind == "write").Path.ShouldEqual(_relativePath);
    [Fact] void should_report_no_refusals() => _result.Refused.ShouldEqual(0);
    [Fact] void should_leave_every_file_and_directory_unchanged() => Snapshot(_destination).ShouldEqual(_before);
}
