// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_CratisLevelBuilder;

public class when_removing_slices_without_an_agent : Specification
{
    string _root;
    string _outcome;
    string _project;

    void Establish() => _root = Path.Combine(Path.GetTempPath(), $"cratis-level-{Guid.NewGuid():N}");

    void Destroy() => Directory.Delete(_root, true);

    async Task Because()
    {
        _project = Path.Combine(_root, "project");
        Directory.CreateDirectory(Path.Combine(_project, "src"));
        await File.WriteAllTextAsync(Path.Combine(_project, "src", "CancelInvoice.cs"), "class CancelInvoice;");
        var orderFile = Path.Combine(_root, "order.json");
        _outcome = Path.Combine(_root, "outcome.json");
        await File.WriteAllTextAsync(orderFile, given.an_order.RemovalJson());
        var session = given.a_session.With() with { Project = _project };
        var environment = new Dictionary<string, string> { [ExecGenerator.OrderVariable] = orderFile, [ExecGenerator.OutcomeVariable] = _outcome };
        await using var builder = new CratisLevelBuilder(session, BuildLevel.Code, "cratis", null);
        await builder.Prompt("build", environment, CancellationToken.None);
    }

    [Fact] void should_report_it_blocked() => JsonNode.Parse(File.ReadAllText(_outcome))!["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Blocked);
    [Fact] void should_say_why() => JsonNode.Parse(File.ReadAllText(_outcome))!["reason"]!.GetValue<string>().ShouldEqual(BuildRemoval.UnresolvableReason);
    [Fact] void should_answer_with_the_contract_of_the_order() => JsonNode.Parse(File.ReadAllText(_outcome))!["contractVersion"]!.GetValue<string>().ShouldEqual("3");
    [Fact] void should_delete_nothing() => File.Exists(Path.Combine(_project, "src", "CancelInvoice.cs")).ShouldBeTrue();
    [Fact] void should_write_no_model() => Directory.Exists(Path.Combine(_project, "screenplay")).ShouldBeFalse();
}
