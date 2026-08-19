// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GeneratedContextVersion;

/// <summary>
/// A file with no version predates stamping or was hand written, so it is deliberately not reported as
/// stale - a warning nobody can act on is how people learn to ignore warnings.
/// </summary>
public class when_deciding_whether_it_is_stale : Specification
{
    [Fact] void should_be_stale_when_generated_by_an_older_cli() =>
        GeneratedContextVersion.IsStale("2.8.2.0", "2.9.0.0").ShouldBeTrue();

    [Fact] void should_be_stale_when_generated_by_a_newer_cli() =>
        GeneratedContextVersion.IsStale("2.9.0.0", "2.8.2.0").ShouldBeTrue();

    [Fact] void should_not_be_stale_when_the_versions_match() =>
        GeneratedContextVersion.IsStale("2.8.2.0", "2.8.2.0").ShouldBeFalse();

    [Fact] void should_not_be_stale_when_the_file_carries_no_version() =>
        GeneratedContextVersion.IsStale(null, "2.8.2.0").ShouldBeFalse();

    [Fact] void should_not_be_stale_for_a_blank_version() =>
        GeneratedContextVersion.IsStale("   ", "2.8.2.0").ShouldBeFalse();
}
