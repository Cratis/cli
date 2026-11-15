// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_building_on_a_branch_the_kit_names : given.a_project_repository
{
    JsonObject _outcome;

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline);

    [Fact] void should_report_built() => _outcome["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Built);
    [Fact] void should_commit_on_the_kits_branch() => Git("log", "-1", "--format=%s", "build/invoicing").ShouldEqual("Build PayInvoice");
    [Fact] void should_report_the_commit() => _outcome["commit"]!.GetValue<string>().ShouldEqual(Git("rev-parse", "build/invoicing"));
    [Fact] void should_report_the_branch() => _outcome["branch"]!.GetValue<string>().ShouldEqual("build/invoicing");
    [Fact] void should_commit_only_the_slices_file() => Git("show", "--name-only", "--format=", "build/invoicing").ShouldEqual("src/PayInvoice.txt");
    [Fact] void should_return_to_the_branch_it_started_on() => Git("branch", "--show-current").ShouldEqual("main");
    [Fact] void should_say_it_verified() => _log.ShouldContain("Verifying: test -f src/PayInvoice.txt");
}
