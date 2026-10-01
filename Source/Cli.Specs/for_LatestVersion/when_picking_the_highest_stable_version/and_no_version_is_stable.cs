// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_picking_the_highest_stable_version;

/// <summary>
/// Nothing to report when the index holds no stable version it can read.
/// </summary>
public class and_no_version_is_stable : Specification
{
    string? _result;

    void Because() => _result = LatestVersion.HighestStable(["3.20.0-preview.1", "latest"]);

    [Fact] void should_have_nothing_to_report() => _result.ShouldBeNull();
}
