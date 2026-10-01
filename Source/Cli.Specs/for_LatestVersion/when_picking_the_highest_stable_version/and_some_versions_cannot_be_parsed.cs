// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_picking_the_highest_stable_version;

/// <summary>
/// What does not parse as a version is skipped, wherever it sits.
/// </summary>
public class and_some_versions_cannot_be_parsed : Specification
{
    string? _result;

    void Because() => _result = LatestVersion.HighestStable(["3.18.0", "not-a-version", "latest", null, "3.19.0", "garbage"]);

    [Fact] void should_pick_the_highest_version_that_parses() => _result.ShouldEqual("3.19.0");
}
