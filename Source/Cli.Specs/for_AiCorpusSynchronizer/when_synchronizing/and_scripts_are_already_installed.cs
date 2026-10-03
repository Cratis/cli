// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_scripts_are_already_installed : given.a_corpus_with_executable_scripts
{
    Dictionary<string, string> _installed = null!;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);
        _installed = _expected.ToDictionary(pair => pair.Key, pair => File.ReadAllText(Installed(pair.Key)));
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);

    [Fact] void should_update_without_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_leave_script_content_unchanged() => _expected.ToDictionary(pair => pair.Key, pair => File.ReadAllText(Installed(pair.Key))).ShouldEqual(_installed);
    [Fact] void should_keep_exactly_one_marker_after_the_shebang() => _expected.ToDictionary(pair => pair.Key, pair => File.ReadAllText(Installed(pair.Key))).ShouldEqual(_expected);
    [Fact] void should_record_matching_content_hashes() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
