// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImage;

public class when_reading_the_local_versions : Specification
{
    IReadOnlyDictionary<string, string> _result;

    void Because() => _result = BuilderImage.LatestVersionsFrom(
        "cratis/builder 1.2.0\ncratis/builder latest\ncratis/builder 1.10.0\ncratis/studio-axon-builder latest\ncratis/studio-eventmodelers-builder 2.0.0\n");

    [Fact] void should_read_the_newest_version_of_each_repository() => _result["cratis/builder"].ShouldEqual("1.10.0");
    [Fact] void should_read_another_repository_on_its_own() => _result["cratis/studio-eventmodelers-builder"].ShouldEqual("2.0.0");
    [Fact] void should_leave_out_a_repository_with_only_moving_tags() => _result.ContainsKey("cratis/studio-axon-builder").ShouldBeFalse();
}
