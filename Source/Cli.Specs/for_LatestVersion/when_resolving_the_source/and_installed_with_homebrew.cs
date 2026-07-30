// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_resolving_the_source;

/// <summary>
/// Homebrew upgrades from the tap, which the release workflow writes from a GitHub release - never NuGet.
/// </summary>
public class and_installed_with_homebrew : Specification
{
    LatestVersionSource _result;

    void Because() => _result = LatestVersion.SourceFor(CliUpdateStrategy.Homebrew);

    [Fact] void should_read_from_the_github_release() => _result.ShouldEqual(LatestVersionSource.GitHubRelease);
}
