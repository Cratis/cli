// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_loading_and_selecting_fixtures : given.a_conformance_run
{
    IReadOnlyList<AiTestFixtureSource> _loaded = [];
    bool _profileMismatch;
    bool _languageMismatch;
    bool _nameMismatch;
    bool _unrestricted;

    async Task Because()
    {
        await Run();
        _loaded = AiTestFixture.Load(Path.GetDirectoryName(_fixtureDirectory)!, new(["claude"], ["cratis/example"], ["csharp"]), ["example"]);
        _profileMismatch = _fixture.Matches(new([], ["other"], ["csharp"]), []);
        _languageMismatch = _fixture.Matches(new([], ["cratis/example"], ["other"]), []);
        _nameMismatch = _fixture.Matches(new([], ["cratis/example"], ["csharp"]), ["other"]);
        _unrestricted = (_fixture with { Profiles = [], Languages = [] }).Matches(new([], [], []), []);
    }
    [Fact] void should_load_selected_fixture() => _loaded.Single().Fixture.Name.ShouldEqual("example");
    [Fact] void should_filter_profiles() => _profileMismatch.ShouldBeFalse();
    [Fact] void should_filter_languages() => _languageMismatch.ShouldBeFalse();
    [Fact] void should_filter_names() => _nameMismatch.ShouldBeFalse();
    [Fact] void should_allow_empty_restrictions() => _unrestricted.ShouldBeTrue();
    [Fact] void should_default_phase_to_result() => _loaded.Single().Fixture.Checks.Single().Phase.ShouldEqual(AiTestPhase.Result);
}
