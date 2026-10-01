// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_picking_the_highest_stable_version;

/// <summary>
/// The position of a version in the list says nothing about it.
/// </summary>
public class and_the_index_is_not_in_order : Specification
{
    string? _result;

    void Because() => _result = LatestVersion.HighestStable(["3.2.0", "3.10.0", "3.9.1", "2.30.0"]);

    [Fact] void should_pick_the_highest_version() => _result.ShouldEqual("3.10.0");
}
