// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusProfiles.when_listing;

public class and_the_corpus_has_no_profile_catalog : given.a_corpus
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => AiCorpusProfiles.Listed(_corpus));

    [Fact] void should_fail_with_an_invalid_operation() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
