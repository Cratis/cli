// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunStackFiles;

public class when_writing_the_files_of_a_run : Specification
{
    DirectoryInfo _runDirectory;
    string _secret;
    string _content;
    UnixFileMode _directoryMode;
    UnixFileMode _fileMode;
    bool _existsAfterDelete;

    async Task Because()
    {
        _runDirectory = RunStackFiles.CreateRunDirectory();
        var plan = new RunStackPlan("network", new("network", "stage", "127.0.0.1", [], []), ["authproxy", "authproxy/secrets"], [new("authproxy/secrets/key", "the value")], [], [], "http://localhost:8080", null);
        await RunStackFiles.Write(_runDirectory.FullName, plan, CancellationToken.None);
        _secret = Path.Combine(_runDirectory.FullName, "authproxy", "secrets", "key");
        _content = await File.ReadAllTextAsync(_secret);
        if (!OperatingSystem.IsWindows())
        {
            _directoryMode = File.GetUnixFileMode(Path.GetDirectoryName(_secret));
            _fileMode = File.GetUnixFileMode(_secret);
        }

        RunStackFiles.Delete(_runDirectory.FullName);
        _existsAfterDelete = Directory.Exists(_runDirectory.FullName);
    }

    [Fact] void should_write_the_value_to_the_file() => _content.ShouldEqual("the value");
    [Fact] void should_let_only_the_owner_read_and_write_the_file() { if (!OperatingSystem.IsWindows()) _fileMode.ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite); }
    [Fact] void should_let_only_the_owner_open_the_directory() { if (!OperatingSystem.IsWindows()) _directoryMode.ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); }
    [Fact] void should_remove_everything_at_the_end() => _existsAfterDelete.ShouldBeFalse();
}
