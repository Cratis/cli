// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSource.when_resolving;

public class and_a_source_is_given : given.a_temporary_root
{
    bool _downloaded;
    string _path;

    void Establish() => Directory.CreateDirectory(_root);

    void Because()
    {
        using var corpus = AiCorpusSource.Resolve(_root, () =>
        {
            _downloaded = true;
            return string.Empty;
        });
        _path = corpus.Path;
    }

    [Fact] void should_use_the_given_source() => _path.ShouldEqual(_root);
    [Fact] void should_not_download() => _downloaded.ShouldBeFalse();
    [Fact] void should_not_delete_the_given_source_once_finished() => Directory.Exists(_root).ShouldBeTrue();
}
