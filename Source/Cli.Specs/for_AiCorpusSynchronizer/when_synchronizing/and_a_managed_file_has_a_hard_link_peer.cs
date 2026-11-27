// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_managed_file_has_a_hard_link_peer : given.a_corpus_with_profile_scoped_rules
{
    [Fact]
    void should_update_the_managed_file_without_changing_its_peer()
    {
        var configuration = new AiConfiguration([], ["cratis/example"], []);
        AiCorpusSynchronizer.Synchronize(_project, _corpus, configuration);
        var installed = Installed("general.md");
        var original = File.ReadAllBytes(installed);
        var peer = Path.Combine(_corpus, "peer.md");
        var command = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("fsutil.exe") { ArgumentList = { "hardlink", "create", peer, installed } }
            : new ProcessStartInfo("ln") { ArgumentList = { installed, peer } };
        using var link = Process.Start(command)!;
        link.WaitForExit();
        Assert.SkipWhen(OperatingSystem.IsWindows() && link.ExitCode != 0, "Cannot create hard links on this Windows installation.");
        link.ExitCode.ShouldEqual(0);
        File.AppendAllText(Path.Combine(_corpus, AiCorpusSynchronizer.ManagedRoot, "rules", "general.md"), "\nUpdated rule");

        var result = AiCorpusSynchronizer.Synchronize(_project, _corpus, configuration);

        result.Conflicts.ShouldBeEmpty();
        File.ReadAllText(installed).ShouldContain("Updated rule");
        File.ReadAllBytes(peer).ShouldEqual(original);
    }
}
