// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class with_corrupt_identities : given.a_persisted_model_root
{
    void Establish() => File.WriteAllText(Path.Combine(_root, ".screenplay", "identities.json"), "{}");
    async Task Because() => _result = await _planning.Plan(new(_root, "Model", "cratis"), CancellationToken.None);

    [Fact] void should_refuse() => _result.Success.ShouldBeFalse();
    [Fact] void should_name_the_identity_conflict() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "CLI-RENDER-017" && diagnostic.Message.StartsWith("IdentityStateConflict: ", StringComparison.Ordinal));
    [Fact] void should_not_fall_back_to_temporary_identities() => _requests.ShouldBeEmpty();
    [Fact] void should_leave_the_identity_file_alone() => File.ReadAllText(Path.Combine(_root, ".screenplay", "identities.json")).ShouldEqual("{}");
}
