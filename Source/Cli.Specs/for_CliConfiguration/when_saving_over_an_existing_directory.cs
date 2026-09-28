// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliConfiguration;

[Collection(CliSpecsCollection.Name)]
public class when_saving_over_an_existing_directory : given.a_temp_config_directory
{
    UnixFileMode _mode;

    void Establish()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var directory = Path.GetDirectoryName(CliConfiguration.GetConfigPath())!;
        Directory.CreateDirectory(directory);
        File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }

    void Because()
    {
        new CliConfiguration().Save();
        if (!OperatingSystem.IsWindows())
        {
            _mode = File.GetUnixFileMode(Path.GetDirectoryName(CliConfiguration.GetConfigPath())!);
        }
    }

    [Fact] void should_restrict_the_directory_to_the_user()
    {
        if (!OperatingSystem.IsWindows())
        {
            _mode.ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
