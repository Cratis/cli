// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_script_permissions_are_dry_run : given.a_corpus_with_script_permissions
{
    UnixFileMode _after;
    string _before = null!;
    string _afterContent = null!;
    SyncResult _performed = null!;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);
        _before = File.ReadAllText(Installed("executable.sh"));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(Installed("executable.sh"), ReadWrite);
    }

    void Because()
    {
        _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration, dryRun: true);
        _afterContent = File.ReadAllText(Installed("executable.sh"));
        if (!OperatingSystem.IsWindows()) _after = File.GetUnixFileMode(Installed("executable.sh"));
        _performed = AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);
    }

    [Fact] void should_not_change_content() => _afterContent.ShouldEqual(_before);
    [Fact] void should_not_change_permissions()
    {
        if (!OperatingSystem.IsWindows()) _after.ShouldEqual(ReadWrite);
    }
    [Fact] void should_report_the_same_actions_as_a_real_update() => _result.Actions.ShouldEqual(_performed.Actions);
    [Fact] void should_report_the_script_update() => _result.Actions.ShouldContain("Updated hooks/scripts/executable.sh");
}
