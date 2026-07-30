// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_resolving_the_source;

public class and_installed_as_a_linux_binary : Specification
{
    LatestVersionSource _result;

    void Because() => _result = LatestVersion.SourceFor(CliUpdateStrategy.ManualLinux);

    [Fact] void should_read_from_the_github_release() => _result.ShouldEqual(LatestVersionSource.GitHubRelease);
}
