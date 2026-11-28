// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusProfiles.when_listing;

public class and_the_catalog_features_profiles : given.a_corpus_that_features_profiles
{
    IReadOnlyList<AiProfile> _profiles = null!;
    IReadOnlyList<AiProfile> _foregrounded = null!;

    void Because()
    {
        _profiles = AiCorpusProfiles.Listed(_corpus);
        _foregrounded = AiCorpusProfiles.Foregrounded(_profiles);
    }

    [Fact] void should_still_list_every_profile() => _profiles.Select(profile => profile.Id).ShouldEqual(["cratis/application", "cratis/application/csharp", "cratis/arc/core", "cratis/engineering"]);
    [Fact] void should_foreground_only_the_featured_profiles() => _foregrounded.Select(profile => profile.Id).ShouldEqual(["cratis/application", "cratis/engineering"]);
    [Fact] void should_carry_what_each_featured_profile_is_for() => _foregrounded.Select(profile => profile.Description).ShouldEqual([ApplicationDescription, EngineeringDescription]);
    [Fact] void should_leave_an_undescribed_profile_without_a_description() => _profiles[1].Description.ShouldBeNull();
}
