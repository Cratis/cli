// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildOutcomes;

public class when_reading_what_an_agent_wrote : Specification
{
    BuildOrder _order;
    JsonObject _built;
    JsonObject _blocked;
    JsonObject _nothing;
    JsonObject _missing;
    JsonObject _garbled;
    JsonObject _unknown;

    void Establish() => _order = given.an_order.Parse();

    void Because()
    {
        var specifications = $$"""[ { "id": "{{given.an_order.SpecificationId}}", "result": "passed", "note": "x" }, { "id": "x", "result": "great" } ]""";
        _built = BuildOutcomes.FromAgent(_order, $$"""{ "kind": "built", "files": ["src/A.cs"], "commit": "abc", "branch": "agent", "pullRequest": "x", "extra": 1, "specifications": {{specifications}}, "assumptions": ["Paid invoices are refused"] }""");
        _blocked = BuildOutcomes.FromAgent(_order, """{ "kind": "blocked", "reason": "Unclear", "questions": ["Which currency?"] }""");
        _nothing = BuildOutcomes.FromAgent(_order, """{ "kind": "nothingToDo", "files": [] }""");
        _missing = BuildOutcomes.FromAgent(_order, null);
        _garbled = BuildOutcomes.FromAgent(_order, "{ not json");
        _unknown = BuildOutcomes.FromAgent(_order, """{ "kind": "done" }""");
    }

    [Fact] void should_answer_the_order() => _built["orderId"]!.GetValue<string>().ShouldEqual(given.an_order.OrderId);
    [Fact] void should_write_contract_version_one() => _built["contractVersion"]!.GetValue<string>().ShouldEqual("1");
    [Fact] void should_keep_only_the_members_built_declares() => _built.Select(pair => pair.Key).ToArray().ShouldEqual(["contractVersion", "orderId", "kind", "files", "specifications", "assumptions"]);
    [Fact] void should_leave_the_git_members_to_the_host() => _built.ContainsKey("commit").ShouldBeFalse();
    [Fact] void should_keep_only_valid_specification_results() => _built["specifications"]!.AsArray().Count.ShouldEqual(1);
    [Fact] void should_drop_undeclared_specification_members() => _built["specifications"]![0]!.AsObject().ContainsKey("note").ShouldBeFalse();
    [Fact] void should_keep_the_questions_of_a_blocked_outcome() => _blocked["questions"]![0]!.GetValue<string>().ShouldEqual("Which currency?");
    [Fact] void should_keep_nothing_to_do_bare() => _nothing.Count.ShouldEqual(3);
    [Fact] void should_fail_retryably_when_the_agent_wrote_nothing() => (_missing["kind"]!.GetValue<string>(), _missing["retryable"]!.GetValue<bool>()).ShouldEqual((BuildOutcomes.Failed, true));
    [Fact] void should_fail_when_the_agent_wrote_no_json() => _garbled["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Failed);
    [Fact] void should_fail_for_an_unknown_kind() => _unknown["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Failed);
}
