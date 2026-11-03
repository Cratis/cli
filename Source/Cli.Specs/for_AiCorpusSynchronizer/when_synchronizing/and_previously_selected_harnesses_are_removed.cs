// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_previously_selected_harnesses_are_removed : given.a_corpus_for_all_harnesses
{
    AiStatus _status = null!;

    void Establish() => AiCorpusSynchronizer.Synchronize(_project, _corpus, AllHarnesses);

    void Because()
    {
        _status = AiCorpusSynchronizer.Status(_project);
        _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, AllHarnesses with { Harnesses = [] });
    }

    [Fact] void should_accept_the_original_producer_pairs() => _status.ModifiedFiles.ShouldBeEmpty();
    [Fact] void should_allow_safe_stale_cleanup() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_remove_the_stale_links() => new DirectoryInfo(Path.Combine(_project, ".pi", "skills")).LinkTarget.ShouldBeNull();
    [Fact] void should_preserve_managed_payloads() => File.Exists(Installed("general.md")).ShouldBeTrue();
}
