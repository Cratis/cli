// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusProfiles.when_selecting;

public class a_profile_the_catalog_does_not_feature : given.a_corpus_that_features_profiles
{
    HashSet<string> _selected = null!;

    void Because() => _selected = AiCorpusProfiles.Selected(_corpus, new([], ["cratis/application/csharp"], ["csharp"]));

    [Fact] void should_resolve_it_with_what_it_composes() => _selected.Order().ShouldEqual(["cratis/application/csharp", "cratis/arc/core"]);
}
