// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSources.when_comparing;

public class with_different_physical_path_casing : Specification
{
    bool _result;

    void Because() => _result = ScreenplayProjectSources.PhysicalPathComparer.Equals(
        "C:/Workspace/Application.csproj",
        "c:/workspace/application.csproj");

    [Fact] void should_follow_the_platforms_physical_path_identity() => _result.ShouldEqual(OperatingSystem.IsWindows());
}
