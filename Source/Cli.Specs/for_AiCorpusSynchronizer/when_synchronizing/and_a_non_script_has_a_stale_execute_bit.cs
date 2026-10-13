// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_non_script_has_a_stale_execute_bit : given.a_corpus_with_script_permissions
{
    UnixFileMode _afterDryRun;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(Installed("library.mjs"), ReadWrite | ExecuteBits);
    }

    void Because()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration, dryRun: true);
        if (!OperatingSystem.IsWindows()) _afterDryRun = File.GetUnixFileMode(Installed("library.mjs"));
        _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);
    }

    [Fact] void should_update_without_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_leave_the_mode_alone_on_a_dry_run()
    {
        if (!OperatingSystem.IsWindows()) _afterDryRun.ShouldEqual(ReadWrite | ExecuteBits);
    }
    [Fact] void should_remove_the_execute_bits_the_corpus_does_not_ship()
    {
        if (!OperatingSystem.IsWindows()) File.GetUnixFileMode(Installed("library.mjs")).ShouldEqual(ReadWrite);
    }
}
