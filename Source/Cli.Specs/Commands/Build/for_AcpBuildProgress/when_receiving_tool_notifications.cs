// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_AcpBuildProgress;

public class when_receiving_tool_notifications : Specification
{
    readonly List<BuildProgress> _progress = [];

    void Because()
    {
        AcpBuildProgress.Publish("session", Notification("session", "tool_call", "Reading the project", "in_progress"), _progress.Add);
        AcpBuildProgress.Publish("session", Notification("session", "tool_call_update", "Reading finished", "completed"), _progress.Add);
        AcpBuildProgress.Publish("session", Notification("other", "tool_call", "Wrong session", "pending"), _progress.Add);
        AcpBuildProgress.Publish("session", Notification("session", "tool_call", " ", "pending"), _progress.Add);
        AcpBuildProgress.Publish("session", Notification("session", "agent_thought_chunk", "Private thought", "pending"), _progress.Add);
        AcpBuildProgress.Publish("session", JsonValue.Create("malformed"), _progress.Add);
        AcpBuildProgress.Publish("session", Notification("session", "tool_call", "Queued", "pending"), _progress.Add);
        AcpBuildProgress.Publish("session", Notification("session", "tool_call_update", "Attempt failed", "failed"), _progress.Add);
        AcpBuildProgress.Publish("session", Notification("session", "tool_call_update", "Noncanonical status", "COMPLETED"), _progress.Add);
    }

    [Fact] void should_only_forward_active_session_tool_titles() => _progress.Count.ShouldEqual(5);
    [Fact] void should_include_tool_status() => _progress[0].Step.ShouldEqual("Reading the project (in_progress)");
    [Fact] void should_observe_title_changes() => _progress[1].Step.ShouldEqual("Reading finished (completed)");
    [Fact] void should_include_pending_status() => _progress[2].Step.ShouldEqual("Queued (pending)");
    [Fact] void should_include_failed_status() => _progress[3].Step.ShouldEqual("Attempt failed (failed)");
    [Fact] void should_match_statuses_ordinally() => _progress[4].Step.ShouldEqual("Noncanonical status");
    [Fact] void should_not_forward_raw_inputs_outputs_or_content() => _progress.Exists(item => item.Step!.Contains("private-content", StringComparison.Ordinal)).ShouldBeFalse();

    static JsonObject Notification(string session, string type, string title, string status) => new()
    {
        ["sessionId"] = session,
        ["update"] = new JsonObject
        {
            ["sessionUpdate"] = type, ["toolCallId"] = "call", ["title"] = title, ["status"] = status,
            ["rawInput"] = "private-content", ["rawOutput"] = "private-content", ["content"] = new JsonArray("private-content")
        }
    };
}
