// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusProfiles.when_listing;

public class and_both_catalog_sections_have_profiles : given.a_corpus
{
    IReadOnlyList<AiProfile> _profiles = null!;

    void Establish() => WriteCatalog("""
        {"schemaVersion":"1.0","publicProfiles":[{"id":"cratis/application","composes":["cratis/documentation"],"languages":["csharp","typescript"]},{"id":"cratis/documentation","languages":["language-agnostic"]}],"engineeringProfiles":[{"id":"cratis/engineering/csharp","composes":["cratis/documentation"],"languages":["csharp"]}],"$cratisAiManaged":{}}
        """);

    void Because() => _profiles = AiCorpusProfiles.Listed(_corpus);

    [Fact] void should_list_every_profile_in_catalog_order() => _profiles.Select(profile => profile.Id).ShouldEqual(["cratis/application", "cratis/documentation", "cratis/engineering/csharp"]);
    [Fact] void should_mark_public_profiles() => _profiles.Take(2).Select(profile => profile.Catalog).ShouldEqual(["public", "public"]);
    [Fact] void should_mark_engineering_profiles() => _profiles[2].Catalog.ShouldEqual("engineering");
    [Fact] void should_carry_languages() => _profiles[0].Languages.ShouldEqual("csharp", "typescript");
    [Fact] void should_carry_composed_profiles() => _profiles[0].Composes.ShouldEqual("cratis/documentation");
    [Fact] void should_leave_composes_empty_when_none_are_declared() => _profiles[1].Composes.ShouldBeEmpty();
}
