// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_scripts_have_shebangs : given.a_corpus_with_executable_scripts
{
    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);

    [Fact] void should_install_without_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_preserve_shebangs_and_line_endings() => _expected.ToDictionary(pair => pair.Key, pair => File.ReadAllText(Installed(pair.Key))).ShouldEqual(_expected);
    [Fact] void should_record_matching_content_hashes() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
