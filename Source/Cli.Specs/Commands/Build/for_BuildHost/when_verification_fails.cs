// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_verification_fails : given.a_project_repository
{
    JsonObject _outcome;

    void Establish() => _fileContent = "nothing";

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline);

    [Fact] void should_report_failed_whatever_the_agent_wrote() => _outcome["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Failed);
    [Fact] void should_let_studio_retry() => _outcome["retryable"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_name_the_failing_command() => _outcome["error"]!.GetValue<string>().ShouldContain("grep -q built src/PayInvoice.txt");
    [Fact] void should_commit_nothing() => Git("rev-list", "--count", "build/invoicing").ShouldEqual("1");
    [Fact] void should_put_the_work_away_rather_than_discard_it() => Git("stash", "list").ShouldContain("left over from order");
    [Fact] void should_leave_the_project_clean() => Git("status", "--porcelain").ShouldEqual(string.Empty);
}
