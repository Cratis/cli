// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_CratisLevelBuilder;

public class when_building_at_the_screenplay_level : Specification
{
    string _root;
    string _outcome;
    BuildAgentReply _reply;

    void Establish() => _root = Path.Combine(Path.GetTempPath(), $"cratis-level-{Guid.NewGuid():N}");

    void Destroy() => Directory.Delete(_root, true);

    async Task Because()
    {
        var project = Path.Combine(_root, "project");
        Directory.CreateDirectory(project);
        var orderFile = Path.Combine(_root, "order.json");
        _outcome = Path.Combine(_root, "outcome.json");
        await File.WriteAllTextAsync(orderFile, given.an_order.Json());
        var session = given.a_session.With() with { Project = project };
        var environment = new Dictionary<string, string> { [ExecGenerator.OrderVariable] = orderFile, [ExecGenerator.OutcomeVariable] = _outcome };
        await using var builder = new CratisLevelBuilder(session, BuildLevel.Screenplay, "cratis", null);
        _reply = await builder.Prompt("build", environment, CancellationToken.None);
        Project = project;
    }

    string Project { get; set; }

    [Fact] void should_succeed() => _reply.Succeeded.ShouldBeTrue();
    [Fact] void should_write_the_model_into_the_project() => File.ReadAllText(Path.Combine(Project, "screenplay", "Billing.play")).ShouldEqual("module Billing");
    [Fact] void should_report_the_slices_built() => JsonNode.Parse(File.ReadAllText(_outcome))!["kind"]!.GetValue<string>().ShouldEqual("built");
    [Fact] void should_render_with_the_chosen_target() => CratisLevelBuilder.RenderArguments("m", "Shop", "p", "AXON").ShouldContain("axon");
    [Fact] void should_describe_the_level_as_a_kit() => BuildLevels.KitName(BuildLevel.CodeWithAI, "axon").ShouldEqual("axon-code-ai");
    [Fact] void should_offer_the_ui_for_the_cratis_target() => BuilderManifests.For(null, "x", BuildLevel.Code, "cratis")["parts"]!.AsArray().Count.ShouldEqual(2);
    [Fact] void should_offer_only_the_backend_for_the_axon_target() => BuilderManifests.For(null, "x", BuildLevel.Code, "axon")["parts"]!.AsArray().Count.ShouldEqual(1);
}
