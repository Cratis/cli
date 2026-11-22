// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_a_removal_is_blocked : given.a_project_repository
{
    JsonObject _outcome;
    string _before;

    void Establish()
    {
        _before = Git("rev-parse", "HEAD");
        _generatorCommand = "[ -n \"$STUDIO_BUILD_OUTCOME\" ] || exit 0; printf '%s' '{ \"kind\": \"blocked\", \"reason\": \"cannot tell which files\", \"questions\": [] }' > \"$STUDIO_BUILD_OUTCOME\"";
    }

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline, given.an_order.ParseRemoval());

    [Fact] void should_report_blocked() => _outcome["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Blocked);
    [Fact] void should_commit_nothing() => Git("rev-parse", "HEAD").ShouldEqual(_before);
    [Fact] void should_not_report_a_commit() => _outcome["commit"].ShouldBeNull();
}
