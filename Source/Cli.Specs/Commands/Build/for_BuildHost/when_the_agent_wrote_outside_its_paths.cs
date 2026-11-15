// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_the_agent_wrote_outside_its_paths : given.a_project_repository
{
    JsonObject _outcome;

    void Establish() => File.WriteAllText(Path.Combine(_project, "notes.txt"), "stray");

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline);

    [Fact] void should_still_report_built() => _outcome["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Built);
    [Fact] void should_not_commit_the_stray_file() => Git("show", "--name-only", "--format=", "build/invoicing").ShouldNotContain("notes.txt");
    [Fact] void should_put_the_stray_file_away() => File.Exists(Path.Combine(_project, "notes.txt")).ShouldBeFalse();
}
