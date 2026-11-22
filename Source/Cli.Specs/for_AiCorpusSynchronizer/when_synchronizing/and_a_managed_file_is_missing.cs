// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_managed_file_is_missing : given.a_pi_corpus
{
    string _installed;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
        _installed = File.ReadAllText(Installed("general.md"));
        File.Delete(Installed("general.md"));
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_restore_the_managed_content() => File.ReadAllText(Installed("general.md")).ShouldEqual(_installed);
    [Fact] void should_report_the_restoration() => _result.Actions.ShouldContain("Restored rules/general.md");
    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_succeed() => AiInstallCommand.Write(_result, OutputFormats.Quiet).ShouldEqual(ExitCodes.Success);
}
