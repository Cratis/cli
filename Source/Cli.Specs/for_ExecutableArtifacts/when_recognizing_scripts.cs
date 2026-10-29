// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ExecutableArtifacts;

public class when_recognizing_scripts : Specification
{
    [Theory]
    [InlineData("mvnw", true)]
    [InlineData("backend/gradlew", true)]
    [InlineData("scripts/start.sh", true)]
    [InlineData("mvnw.cmd", false)]
    [InlineData("gradlew.bat", false)]
    [InlineData("pom.xml", false)]
    [InlineData("docs/mvnw.md", false)]
    void should_recognize_wrappers_and_shell_scripts(string relativePath, bool expected) =>
        ExecutableArtifacts.IsScript(relativePath).ShouldEqual(expected);
}
