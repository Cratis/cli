// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_resolving_the_source;

public class and_installed_as_a_dotnet_tool : Specification
{
    LatestVersionSource _result;

    void Because() => _result = LatestVersion.SourceFor(CliUpdateStrategy.DotNetTool);

    [Fact] void should_read_from_nuget() => _result.ShouldEqual(LatestVersionSource.NuGet);
}
