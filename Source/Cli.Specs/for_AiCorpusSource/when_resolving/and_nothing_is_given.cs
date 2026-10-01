// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSource.when_resolving;

public class and_nothing_is_given : given.a_temporary_root
{
    string _downloadedPath;
    bool _existedWhileInUse;

    void Establish()
    {
        _downloadedPath = Path.Combine(_root, "downloaded");
        Directory.CreateDirectory(_downloadedPath);
        File.WriteAllText(Path.Combine(_downloadedPath, "corpus.txt"), "downloaded");
    }

    void Because()
    {
        using var corpus = AiCorpusSource.Resolve(null, () => _downloadedPath);
        _existedWhileInUse = Directory.Exists(corpus.Path);
    }

    [Fact] void should_have_the_downloaded_corpus_available_while_in_use() => _existedWhileInUse.ShouldBeTrue();
    [Fact] void should_delete_the_downloaded_corpus_once_finished() => Directory.Exists(_downloadedPath).ShouldBeFalse();
}
