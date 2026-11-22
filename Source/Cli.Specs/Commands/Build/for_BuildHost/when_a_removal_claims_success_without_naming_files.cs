// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_a_removal_claims_success_without_naming_files : given.a_project_repository
{
    JsonObject _outcome;

    void Establish() => _generatorCommand = "printf '%s' '{ \"kind\": \"built\", \"files\": [], \"specifications\": [], \"assumptions\": [] }' > \"$STUDIO_BUILD_OUTCOME\"";

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline, given.an_order.ParseRemoval());

    [Fact] void should_not_report_built() => _outcome["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Failed);
    [Fact] void should_not_claim_a_cleanup() => _outcome["error"]!.GetValue<string>().ShouldContain("without naming the files");
    [Fact] void should_allow_trying_again() => _outcome["retryable"]!.GetValue<bool>().ShouldBeTrue();
}
