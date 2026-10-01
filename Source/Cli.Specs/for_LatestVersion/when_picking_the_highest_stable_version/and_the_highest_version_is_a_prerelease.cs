// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_picking_the_highest_stable_version;

/// <summary>
/// A prerelease is never the latest stable version.
/// </summary>
public class and_the_highest_version_is_a_prerelease : Specification
{
    string? _result;

    void Because() => _result = LatestVersion.HighestStable(["3.19.0", "3.20.0-preview.1", "3.18.0"]);

    [Fact] void should_pick_the_highest_stable_version() => _result.ShouldEqual("3.19.0");
}
