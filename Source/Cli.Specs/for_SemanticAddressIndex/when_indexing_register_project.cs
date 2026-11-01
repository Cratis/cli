// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_SemanticAddressIndex;

public class when_indexing_register_project : for_ScreenplayPlanning.given.a_canonical_screenplay
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await _planning.Plan(new(WriteSource("single"), Corpus.ApplicationName, "cratis"), CancellationToken.None);

    [Fact] void should_resolve_the_command() => AssertSource("command", "RegisterProject", "Projects/Registration/RegisterProject/RegisterProject");
    [Fact] void should_resolve_the_event() => AssertSource("event", "ProjectRegistered", "Projects/Registration/RegisterProject/ProjectRegistered");
    [Fact] void should_resolve_the_read_model() => AssertSource("readmodel", "ProjectSummary", "Projects/Registration/ProjectLookup/ProjectSummary");
    [Fact] void should_resolve_the_specification() => AssertSource("specification", "RegisteringAProject", "Projects/Registration/RegisterProject/RegisteringAProject");
    [Fact] void should_keep_unknown_identities() => _result.SemanticAddresses.Resolve("unplaced-id").ShouldEqual(new ArtifactSource("unplaced-id", "unknown", null));

    void AssertSource(string kind, string name, string address)
    {
        var application = _requests.Single().Model.Application;
        var slices = application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).ToArray();
        var id = kind switch
        {
            "command" => slices.SelectMany(slice => slice.Commands).Single(declaration => declaration.Name == name).Id,
            "event" => slices.SelectMany(slice => slice.Events).Single(declaration => declaration.Name == name).Id,
            "readmodel" => slices.SelectMany(slice => slice.ReadModels).Single(declaration => declaration.Name == name).Id,
            _ => slices.SelectMany(slice => slice.Specifications).Single(declaration => declaration.Name == name).Id
        };
        _result.SemanticAddresses.Resolve(id.ToString()).ShouldEqual(new ArtifactSource(id.ToString(), kind, address));
    }
}
