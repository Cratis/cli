// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSource.when_resolving;

public class and_the_work_fails_while_the_corpus_is_in_use : given.a_temporary_root
{
    string _downloadedPath;
    Exception? _exception;

    void Establish()
    {
        _downloadedPath = Path.Combine(_root, "downloaded");
        Directory.CreateDirectory(_downloadedPath);
    }

    void Because()
    {
        try
        {
            using var corpus = AiCorpusSource.Resolve(null, () => _downloadedPath);
            throw new InvalidOperationException("synchronization failed");
        }
        catch (InvalidOperationException exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_surface_the_failure() => _exception.ShouldNotBeNull();
    [Fact] void should_still_delete_the_downloaded_corpus() => Directory.Exists(_downloadedPath).ShouldBeFalse();
}
