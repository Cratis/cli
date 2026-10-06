// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_script_permissions_are_installed : given.a_corpus_with_script_permissions
{
    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);

    [Fact] void should_install_without_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_install_all_three_files() => _result.Actions.Count.ShouldEqual(3);
    [Fact] void should_use_source_execute_bits_even_without_a_shebang()
    {
        if (OperatingSystem.IsWindows()) return;
        var mode = File.GetUnixFileMode(Installed("executable.sh"));
        (mode & ExecuteBits).ShouldEqual((UnixFileMode)((int)mode >> 2) & ExecuteBits);
    }
    [Fact] void should_make_a_shebang_script_executable_when_source_execute_bits_are_missing()
    {
        if (OperatingSystem.IsWindows()) return;
        var mode = File.GetUnixFileMode(Installed("shebang.mjs"));
        (mode & ExecuteBits).ShouldEqual((UnixFileMode)((int)mode >> 2) & ExecuteBits);
    }
    [Fact] void should_leave_a_non_script_non_executable()
    {
        if (!OperatingSystem.IsWindows()) (File.GetUnixFileMode(Installed("library.mjs")) & ExecuteBits).ShouldEqual(UnixFileMode.None);
    }
}
