// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusProfiles.when_listing;

public class and_the_catalog_has_no_profile_sections : given.a_corpus
{
    IReadOnlyList<AiProfile> _profiles = null!;

    void Establish() => WriteCatalog("{}");

    void Because() => _profiles = AiCorpusProfiles.Listed(_corpus);

    [Fact] void should_list_nothing() => _profiles.ShouldBeEmpty();
}
