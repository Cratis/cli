// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_building_on_the_branch_studio_names : given.a_project_repository
{
    JsonObject _outcome;

    async Task Because() => _outcome = await Build(new(BuildGitInstructions.StudioManaged, "studio/invoicing/pay-invoice", "Build PayInvoice (backend)"));

    [Fact] void should_commit_on_studios_branch() => _outcome["branch"]!.GetValue<string>().ShouldEqual("studio/invoicing/pay-invoice");
    [Fact] void should_use_studios_commit_message() => Git("log", "-1", "--format=%s", "studio/invoicing/pay-invoice").ShouldEqual("Build PayInvoice (backend)");
}
