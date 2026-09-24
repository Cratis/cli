// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.for_StageImage;

/// <summary>
/// Whether a newer Stage image exists is only worth asking about once this computer already has one - reading
/// what is here first is what makes that possible.
/// </summary>
public class when_reading_the_newest_local_tag : Specification
{
    [Fact] void should_read_a_single_tag() => StageImage.LatestVersionFrom("4.3.0\n").ShouldEqual("4.3.0");

    [Fact] void should_read_the_highest_of_several_tags() =>
        StageImage.LatestVersionFrom("4.1.0\n4.4.0\n4.2.0\n").ShouldEqual("4.4.0");

    [Fact] void should_skip_the_moving_latest_tag() => StageImage.LatestVersionFrom("latest\n4.2.0\n").ShouldEqual("4.2.0");

    /// <summary>
    /// A locally built image carries a git-describe tag such as this, which does not parse as a version and
    /// is not "the version" a published Stage image would be compared against.
    /// </summary>
    [Fact] void should_skip_a_locally_built_tag() => StageImage.LatestVersionFrom("4.0.0-15-g046e3f9-dirty\n4.3.0\n").ShouldEqual("4.3.0");

    [Fact] void should_read_nothing_as_never_pulled() => StageImage.LatestVersionFrom(string.Empty).ShouldBeNull();

    [Fact] void should_read_only_moving_tags_as_never_pulled() => StageImage.LatestVersionFrom("latest\ndev\n").ShouldBeNull();
}
