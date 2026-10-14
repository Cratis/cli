// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.when_checking;

public class with_a_checked_out_state_folder : given.a_release_model
{
    async Task Establish()
    {
        await Run("screenplay", "baseline", "admit", _root, "--release", "1.0.0");
        if (!OperatingSystem.IsWindows())
        {
            // A git checkout creates folders as 0755, not the owner-only mode the MCP server writes.
            File.SetUnixFileMode(
                Path.Combine(_root, ".screenplay"),
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }

    Task Because() => Run("screenplay", "baseline", "check", _root);

    [Fact] void should_check_the_baseline() => _exitCode.ShouldEqual(0);
    [Fact] void should_not_refuse_the_folder_permissions() => _error.ShouldNotContain("MetadataPermissions");
}
