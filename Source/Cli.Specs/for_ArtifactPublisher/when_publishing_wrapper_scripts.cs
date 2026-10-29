// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_publishing_wrapper_scripts : given.a_wrapper_script_publication
{
    Task Because() => Publish();

    [Fact] void should_make_the_maven_wrapper_executable_by_its_owner() => HasOwnerExecute("mvnw").ShouldBeTrue();
    [Fact] void should_make_the_gradle_wrapper_executable_by_its_owner() => HasOwnerExecute("gradlew").ShouldBeTrue();
    [Fact] void should_make_shell_scripts_executable_by_their_owner() => HasOwnerExecute("scripts/start.sh").ShouldBeTrue();
    [Fact] void should_not_make_other_artifacts_executable() => HasAnyExecute("pom.xml").ShouldBeFalse();
    [Fact] void should_only_grant_execute_where_read_is_granted() => ExecuteFollowsRead("mvnw").ShouldBeTrue();

    bool ExecuteFollowsRead(string relativePath)
    {
        if (OperatingSystem.IsWindows())
        {
            return true;
        }

        var mode = File.GetUnixFileMode(ArtifactPath(relativePath));
        return mode.HasFlag(UnixFileMode.GroupExecute) == mode.HasFlag(UnixFileMode.GroupRead) &&
            mode.HasFlag(UnixFileMode.OtherExecute) == mode.HasFlag(UnixFileMode.OtherRead);
    }
}
