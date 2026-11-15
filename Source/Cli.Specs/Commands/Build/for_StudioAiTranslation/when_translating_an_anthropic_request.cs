// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_StudioAiTranslation;

public class when_translating_an_anthropic_request : Specification
{
    JsonObject _request;
    JsonObject _message;

    void Because()
    {
        _request = StudioAiTranslation.FromAnthropic(
            JsonNode.Parse("""{ "model": "claude-x", "max_tokens": 2000, "system": [ { "type": "text", "text": "Be brief." } ], "messages": [ { "role": "user", "content": "Read a.cs" }, { "role": "assistant", "content": [ { "type": "text", "text": "Reading" }, { "type": "tool_use", "id": "t1", "name": "read_file", "input": { "path": "a.cs" } } ] }, { "role": "user", "content": [ { "type": "tool_result", "tool_use_id": "t1", "content": [ { "type": "text", "text": "class A;" } ] } ] } ], "tools": [ { "name": "read_file", "description": "Reads", "input_schema": { "type": "object" } } ] }""")!.AsObject(),
            "order-2");
        _message = StudioAiTranslation.ToAnthropic(
            JsonNode.Parse("""{ "message": { "role": "assistant", "text": "Done" }, "finishReason": "length", "usage": { "inputTokens": 5, "outputTokens": 7 } }""")!.AsObject(),
            "claude-x");
    }

    [Fact] void should_start_with_the_system_prompt() => _request["messages"]![0]!["role"]!.GetValue<string>().ShouldEqual("system");
    [Fact] void should_keep_the_assistants_tool_call() => _request["messages"]![2]!["toolCalls"]![0]!["name"]!.GetValue<string>().ShouldEqual("read_file");
    [Fact] void should_turn_a_tool_result_into_a_tool_message() => _request["messages"]![3]!["role"]!.GetValue<string>().ShouldEqual("tool");
    [Fact] void should_name_the_call_it_answers() => _request["messages"]![3]!["toolCallId"]!.GetValue<string>().ShouldEqual("t1");
    [Fact] void should_read_the_tool_schema() => _request["tools"]![0]!["parameters"]!["type"]!.GetValue<string>().ShouldEqual("object");
    [Fact] void should_answer_with_a_text_block() => _message["content"]![0]!["text"]!.GetValue<string>().ShouldEqual("Done");
    [Fact] void should_stop_for_length() => _message["stop_reason"]!.GetValue<string>().ShouldEqual("max_tokens");
    [Fact] void should_report_usage() => _message["usage"]!["output_tokens"]!.GetValue<long>().ShouldEqual(7);
    [Fact] void should_stream_start_to_stop() => (StudioAiTranslation.AnthropicStream(_message)[0].Event, StudioAiTranslation.AnthropicStream(_message)[^1].Event).ShouldEqual(("message_start", "message_stop"));
}
