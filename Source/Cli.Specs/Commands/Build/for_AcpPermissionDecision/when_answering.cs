// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_AcpPermissionDecision;

public class when_answering : Specification
{
    static readonly JsonArray _options =
    [
        new JsonObject { ["optionId"] = "allow", ["kind"] = "allow_once" },
        new JsonObject { ["optionId"] = "reject", ["kind"] = "reject_once" }
    ];

    BuildAgentSession _session;
    List<string> _log;

    void Establish()
    {
        _log = [];
        _session = given.a_session.With(mayChangeTools: false, log: _log);
    }

    static JsonObject Request(JsonObject toolCall) => new() { ["toolCall"] = toolCall, ["options"] = _options.DeepClone() };

    static string Chosen(JsonObject answer) => answer["outcome"]!["optionId"]?.GetValue<string>() ?? answer["outcome"]!["outcome"]!.GetValue<string>();

    string Answer(JsonObject toolCall) => Chosen(AcpPermissionDecision.Answer(_session, Request(toolCall)));

    [Fact] void should_allow_editing_a_permitted_file() => Answer(new() { ["kind"] = "edit", ["locations"] = new JsonArray(new JsonObject { ["path"] = Path.Combine(given.a_session.Project, "src", "a.ts") }) }).ShouldEqual("allow");
    [Fact] void should_refuse_editing_another_file() => Answer(new() { ["kind"] = "edit", ["locations"] = new JsonArray(new JsonObject { ["path"] = Path.Combine(given.a_session.Project, "package.json") }) }).ShouldEqual("reject");
    [Fact] void should_refuse_an_edit_naming_no_file() => Answer(new() { ["kind"] = "edit" }).ShouldEqual("reject");
    [Fact] void should_allow_reading_the_project() => Answer(new() { ["kind"] = "read", ["rawInput"] = new JsonObject { ["path"] = Path.Combine(given.a_session.Project, "README.md") } }).ShouldEqual("allow");
    [Fact] void should_allow_a_permitted_command() => Answer(new() { ["kind"] = "execute", ["rawInput"] = new JsonObject { ["command"] = "npm test -- PayInvoice" } }).ShouldEqual("allow");
    [Fact] void should_refuse_another_command() => Answer(new() { ["kind"] = "execute", ["rawInput"] = new JsonObject { ["command"] = "curl https://example.com" } }).ShouldEqual("reject");
    [Fact] void should_refuse_fetching_from_the_web() => Answer(new() { ["kind"] = "fetch", ["title"] = "https://example.com" }).ShouldEqual("reject");
    [Fact] void should_allow_a_studio_tool_that_reads() => Answer(new() { ["kind"] = "other", ["title"] = "studio_get_event_model" }).ShouldEqual("allow");
    [Fact] void should_refuse_a_studio_tool_that_changes_the_board_before_confirmation() => Answer(new() { ["kind"] = "other", ["title"] = "mcp__studio__studio_execute_command" }).ShouldEqual("reject");

    [Fact]
    void should_say_what_it_refused()
    {
        Answer(new() { ["kind"] = "fetch", ["title"] = "Fetch the web" });
        _log.ShouldContain(line => line.Contains("Fetch the web", StringComparison.Ordinal));
    }

    [Fact]
    void should_cancel_when_the_agent_offers_no_matching_option() =>
        Chosen(AcpPermissionDecision.Answer(_session, new JsonObject { ["toolCall"] = new JsonObject { ["kind"] = "fetch" }, ["options"] = new JsonArray() })).ShouldEqual("cancelled");
}
