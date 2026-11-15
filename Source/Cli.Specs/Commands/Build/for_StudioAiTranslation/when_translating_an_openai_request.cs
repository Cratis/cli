// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_StudioAiTranslation;

public class when_translating_an_openai_request : Specification
{
    JsonObject _request;
    JsonObject _answer;
    JsonObject _completion;

    void Because()
    {
        _request = StudioAiTranslation.FromOpenAi(
            JsonNode.Parse("""{ "model": "gpt-x", "max_tokens": 4000, "temperature": 0.2, "messages": [ { "role": "system", "content": "Be brief." }, { "role": "user", "content": [ { "type": "text", "text": "Read a.cs" } ] }, { "role": "assistant", "content": null, "tool_calls": [ { "id": "c1", "type": "function", "function": { "name": "read_file", "arguments": "{\"path\":\"a.cs\"}" } } ] }, { "role": "tool", "tool_call_id": "c1", "content": "class A;" } ], "tools": [ { "type": "function", "function": { "name": "read_file", "description": "Reads", "parameters": { "type": "object" } } } ] }""")!.AsObject(),
            "order-1");
        _answer = JsonNode.Parse("""{ "message": { "role": "assistant", "text": "ok", "toolCalls": [ { "id": "c2", "name": "write_file", "arguments": { "path": "b.cs" } } ] }, "finishReason": "toolCalls", "model": "m1", "usage": { "inputTokens": 10, "outputTokens": 4 } }""")!.AsObject();
        _completion = StudioAiTranslation.ToOpenAi(_answer, "gpt-x");
    }

    [Fact] void should_carry_the_order() => _request["orderId"]!.GetValue<string>().ShouldEqual("order-1");
    [Fact] void should_keep_the_messages_in_order() => _request["messages"]!.AsArray().Select(message => message!["role"]!.GetValue<string>()).ShouldContainOnly(["system", "user", "assistant", "tool"]);
    [Fact] void should_flatten_text_parts() => _request["messages"]![1]!["text"]!.GetValue<string>().ShouldEqual("Read a.cs");
    [Fact] void should_parse_tool_call_arguments_into_an_object() => _request["messages"]![2]!["toolCalls"]![0]!["arguments"]!["path"]!.GetValue<string>().ShouldEqual("a.cs");
    [Fact] void should_name_the_call_a_tool_result_answers() => _request["messages"]![3]!["toolCallId"]!.GetValue<string>().ShouldEqual("c1");
    [Fact] void should_carry_the_tools() => _request["tools"]![0]!["name"]!.GetValue<string>().ShouldEqual("read_file");
    [Fact] void should_carry_the_output_limit() => _request["maxOutputTokens"]!.GetValue<int>().ShouldEqual(4000);
    [Fact] void should_answer_with_the_tool_call_as_text_arguments() => _completion["choices"]![0]!["message"]!["tool_calls"]![0]!["function"]!["arguments"]!.GetValue<string>().ShouldEqual("{\"path\":\"b.cs\"}");
    [Fact] void should_finish_with_tool_calls() => _completion["choices"]![0]!["finish_reason"]!.GetValue<string>().ShouldEqual("tool_calls");
    [Fact] void should_report_usage() => _completion["usage"]!["total_tokens"]!.GetValue<long>().ShouldEqual(14);
    [Fact] void should_end_a_stream_with_the_done_marker() => StudioAiTranslation.OpenAiStream(_completion)[^1].ShouldEqual("[DONE]");
}
