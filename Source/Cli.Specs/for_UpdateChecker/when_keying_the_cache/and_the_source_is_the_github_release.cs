// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateChecker.when_keying_the_cache;

/// <summary>
/// Keyed apart from the package so an answer read from NuGet is never served to a native installation.
/// </summary>
public class and_the_source_is_the_github_release : Specification
{
    string _result = null!;

    void Because() => _result = UpdateChecker.CacheKeyFor(LatestVersionSource.GitHubRelease, UpdateChecker.CliPackageId);

    [Fact] void should_not_collide_with_the_package_key() => _result.ShouldNotEqual(UpdateChecker.CliPackageId);
}
