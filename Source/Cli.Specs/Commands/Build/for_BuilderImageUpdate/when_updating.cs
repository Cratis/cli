// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImageUpdate;

public class when_updating : Specification
{
    readonly List<string> _pulled = [];
    IReadOnlyList<BuilderImageUpdateResult> _results;

    async Task Because() => _results = await BuilderImageUpdate.Update(
        new Dictionary<string, string>
        {
            ["cratis/builder"] = "1.4.0",
            ["cratis/studio-axon-builder"] = "1.5.0",
            ["cratis/studio-eventmodelers-builder"] = "1.3.0"
        },
        (repository, _) => Task.FromResult<string?>(repository switch
        {
            "cratis/builder" => "1.5.0",
            "cratis/studio-eventmodelers-builder" => "1.5.0",
            _ => null
        }),
        reference =>
        {
            _pulled.Add(reference);
            return Task.FromResult(!reference.StartsWith("cratis/studio-eventmodelers-builder", StringComparison.Ordinal));
        });

    [Fact] void should_report_every_image_present() => _results.Count.ShouldEqual(3);
    [Fact] void should_pull_the_newer_versions() => _pulled.ShouldContainOnly(["cratis/builder:1.5.0", "cratis/studio-eventmodelers-builder:1.5.0"]);
    [Fact] void should_report_an_image_it_pulled() => _results[0].ShouldEqual(new BuilderImageUpdateResult("cratis/builder", "1.4.0", "1.5.0", true));
    [Fact] void should_report_an_image_already_at_the_latest() => _results[1].ShouldEqual(new BuilderImageUpdateResult("cratis/studio-axon-builder", "1.5.0", "1.5.0", false));
    [Fact] void should_keep_the_local_version_when_the_pull_failed() => _results[2].ShouldEqual(new BuilderImageUpdateResult("cratis/studio-eventmodelers-builder", "1.3.0", "1.3.0", false));
}
