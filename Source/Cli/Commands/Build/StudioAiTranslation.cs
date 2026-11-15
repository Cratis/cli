// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Translates between the dialects coding agents speak (OpenAI chat completions and the Anthropic messages API) and the model
/// call of the builder protocol's AI gateway, so any agent can be pointed at the bridge and have its calls run through Studio.
/// </summary>
public static class StudioAiTranslation
{
    /// <summary>
    /// Translates an OpenAI chat completions request into a gateway request.
    /// </summary>
    /// <param name="request">The agent's request.</param>
    /// <param name="orderId">The order being built.</param>
    /// <returns>The gateway request.</returns>
    public static JsonObject FromOpenAi(JsonObject request, string orderId)
    {
        var messages = new JsonArray();
        foreach (var message in (request["messages"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var role = Text(message["role"]) ?? "user";
            if (string.Equals(role, "developer", StringComparison.Ordinal)) role = "system";
            var translated = new JsonObject { ["role"] = role };
            var text = ContentText(message["content"]);
            if (text is not null) translated["text"] = text;
            if (message["tool_calls"] is JsonArray calls && calls.Count > 0)
            {
                translated["toolCalls"] = new JsonArray([.. calls.OfType<JsonObject>().Select(call => (JsonNode)new JsonObject
                {
                    ["id"] = Text(call["id"]) ?? Guid.NewGuid().ToString("N"),
                    ["name"] = Text(call["function"]?["name"]) ?? string.Empty,
                    ["arguments"] = ParseArguments(Text(call["function"]?["arguments"]))
                })]);
            }

            if (role == "tool") translated["toolCallId"] = Text(message["tool_call_id"]) ?? string.Empty;
            messages.Add(translated);
        }

        var tools = new JsonArray([.. (request["tools"] as JsonArray ?? []).OfType<JsonObject>()
            .Where(tool => tool["function"] is JsonObject)
            .Select(tool => (JsonNode)new JsonObject
            {
                ["name"] = Text(tool["function"]!["name"]) ?? string.Empty,
                ["description"] = Text(tool["function"]!["description"]),
                ["parameters"] = tool["function"]!["parameters"]?.DeepClone()
            })]);
        return Request(orderId, messages, tools, request["max_completion_tokens"] ?? request["max_tokens"], request["temperature"]);
    }

    /// <summary>
    /// Translates a gateway answer into an OpenAI chat completion.
    /// </summary>
    /// <param name="answer">The gateway's answer.</param>
    /// <param name="model">The model the agent asked for.</param>
    /// <returns>The chat completion.</returns>
    public static JsonObject ToOpenAi(JsonObject answer, string model)
    {
        var message = answer["message"] as JsonObject ?? [];
        var assistant = new JsonObject { ["role"] = "assistant", ["content"] = Text(message["text"]) };
        if (message["toolCalls"] is JsonArray { Count: > 0 } calls)
        {
            assistant["tool_calls"] = new JsonArray([.. calls.OfType<JsonObject>().Select(call => (JsonNode)new JsonObject
            {
                ["id"] = Text(call["id"]),
                ["type"] = "function",
                ["function"] = new JsonObject { ["name"] = Text(call["name"]), ["arguments"] = (call["arguments"] ?? new JsonObject()).ToJsonString() }
            })]);
        }

        var input = Tokens(answer, "inputTokens");
        var output = Tokens(answer, "outputTokens");
        return new JsonObject
        {
            ["id"] = $"chatcmpl-{Guid.NewGuid():N}",
            ["object"] = "chat.completion",
            ["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["model"] = Text(answer["model"]) ?? model,
            ["choices"] = new JsonArray(new JsonObject { ["index"] = 0, ["message"] = assistant, ["finish_reason"] = OpenAiFinish(Text(answer["finishReason"])) }),
            ["usage"] = new JsonObject { ["prompt_tokens"] = input, ["completion_tokens"] = output, ["total_tokens"] = input + output }
        };
    }

    /// <summary>
    /// Writes an OpenAI chat completion as the server-sent events of a streamed one.
    /// </summary>
    /// <param name="completion">The completion.</param>
    /// <returns>The events, each as the text of one <c language="csharp">data:</c> line, ending with the done marker.</returns>
    public static IReadOnlyList<string> OpenAiStream(JsonObject completion)
    {
        var choice = (JsonObject)completion["choices"]![0]!;
        var message = (JsonObject)choice["message"]!;
        string Chunk(JsonObject delta, string? finish, JsonObject? usage = null)
        {
            var chunk = new JsonObject
            {
                ["id"] = completion["id"]?.DeepClone(),
                ["object"] = "chat.completion.chunk",
                ["created"] = completion["created"]?.DeepClone(),
                ["model"] = completion["model"]?.DeepClone(),
                ["choices"] = new JsonArray(new JsonObject { ["index"] = 0, ["delta"] = delta, ["finish_reason"] = finish })
            };
            if (usage is not null) chunk["usage"] = usage;
            return chunk.ToJsonString();
        }

        var events = new List<string> { Chunk(new JsonObject { ["role"] = "assistant" }, null) };
        if (Text(message["content"]) is { Length: > 0 } content) events.Add(Chunk(new JsonObject { ["content"] = content }, null));
        if (message["tool_calls"] is JsonArray calls)
        {
            var index = 0;
            foreach (var call in calls.OfType<JsonObject>())
            {
                var toolCall = new JsonObject
                {
                    ["index"] = index++,
                    ["id"] = call["id"]?.DeepClone(),
                    ["type"] = "function",
                    ["function"] = call["function"]!.DeepClone()
                };
                events.Add(Chunk(new JsonObject { ["tool_calls"] = new JsonArray(toolCall) }, null));
            }
        }

        events.Add(Chunk([], Text(choice["finish_reason"]), completion["usage"]?.DeepClone() as JsonObject));
        events.Add("[DONE]");
        return events;
    }

    /// <summary>
    /// Translates an Anthropic messages request into a gateway request.
    /// </summary>
    /// <param name="request">The agent's request.</param>
    /// <param name="orderId">The order being built.</param>
    /// <returns>The gateway request.</returns>
    public static JsonObject FromAnthropic(JsonObject request, string orderId)
    {
        var messages = new JsonArray();
        if (ContentText(request["system"]) is { Length: > 0 } system) messages.Add(new JsonObject { ["role"] = "system", ["text"] = system });
        foreach (var message in (request["messages"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var role = Text(message["role"]) ?? "user";
            if (message["content"] is not JsonArray blocks)
            {
                messages.Add(new JsonObject { ["role"] = role, ["text"] = Text(message["content"]) ?? string.Empty });
                continue;
            }

            var text = new List<string>();
            var calls = new JsonArray();
            foreach (var block in blocks.OfType<JsonObject>())
            {
                switch (Text(block["type"]))
                {
                    case "text":
                        text.Add(Text(block["text"]) ?? string.Empty);
                        break;
                    case "tool_use":
                        calls.Add(new JsonObject { ["id"] = Text(block["id"]), ["name"] = Text(block["name"]), ["arguments"] = block["input"]?.DeepClone() ?? new JsonObject() });
                        break;
                    case "tool_result":
                        messages.Add(new JsonObject { ["role"] = "tool", ["toolCallId"] = Text(block["tool_use_id"]) ?? string.Empty, ["text"] = ContentText(block["content"]) ?? string.Empty });
                        break;
                }
            }

            if (text.Count == 0 && calls.Count == 0) continue;
            var translated = new JsonObject { ["role"] = role, ["text"] = string.Join('\n', text) };
            if (calls.Count > 0) translated["toolCalls"] = calls;
            messages.Add(translated);
        }

        var tools = new JsonArray([.. (request["tools"] as JsonArray ?? []).OfType<JsonObject>().Select(tool => (JsonNode)new JsonObject
        {
            ["name"] = Text(tool["name"]) ?? string.Empty,
            ["description"] = Text(tool["description"]),
            ["parameters"] = tool["input_schema"]?.DeepClone()
        })]);
        return Request(orderId, messages, tools, request["max_tokens"], request["temperature"]);
    }

    /// <summary>
    /// Translates a gateway answer into an Anthropic message.
    /// </summary>
    /// <param name="answer">The gateway's answer.</param>
    /// <param name="model">The model the agent asked for.</param>
    /// <returns>The message.</returns>
    public static JsonObject ToAnthropic(JsonObject answer, string model)
    {
        var message = answer["message"] as JsonObject ?? [];
        var content = new JsonArray();
        if (Text(message["text"]) is { Length: > 0 } text) content.Add(new JsonObject { ["type"] = "text", ["text"] = text });
        foreach (var call in (message["toolCalls"] as JsonArray ?? []).OfType<JsonObject>())
        {
            content.Add(new JsonObject { ["type"] = "tool_use", ["id"] = Text(call["id"]), ["name"] = Text(call["name"]), ["input"] = call["arguments"]?.DeepClone() ?? new JsonObject() });
        }

        if (content.Count == 0) content.Add(new JsonObject { ["type"] = "text", ["text"] = string.Empty });
        return new JsonObject
        {
            ["id"] = $"msg_{Guid.NewGuid():N}",
            ["type"] = "message",
            ["role"] = "assistant",
            ["model"] = Text(answer["model"]) ?? model,
            ["content"] = content,
            ["stop_reason"] = Text(answer["finishReason"]) switch { "toolCalls" => "tool_use", "length" => "max_tokens", _ => "end_turn" },
            ["stop_sequence"] = null,
            ["usage"] = new JsonObject { ["input_tokens"] = Tokens(answer, "inputTokens"), ["output_tokens"] = Tokens(answer, "outputTokens") }
        };
    }

    /// <summary>
    /// Writes an Anthropic message as the server-sent events of a streamed one.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The events as (event name, data) pairs.</returns>
    public static IReadOnlyList<(string Event, string Data)> AnthropicStream(JsonObject message)
    {
        var events = new List<(string, string)>();
        var start = (JsonObject)message.DeepClone();
        start["content"] = new JsonArray();
        start["stop_reason"] = null;
        start["usage"] = new JsonObject { ["input_tokens"] = message["usage"]?["input_tokens"]?.DeepClone(), ["output_tokens"] = 0 };
        events.Add(("message_start", new JsonObject { ["type"] = "message_start", ["message"] = start }.ToJsonString()));
        var index = 0;
        foreach (var block in (message["content"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var empty = (JsonObject)block.DeepClone();
            JsonObject delta;
            if (Text(block["type"]) == "tool_use")
            {
                empty["input"] = new JsonObject();
                delta = new JsonObject { ["type"] = "input_json_delta", ["partial_json"] = (block["input"] ?? new JsonObject()).ToJsonString() };
            }
            else
            {
                empty["text"] = string.Empty;
                delta = new JsonObject { ["type"] = "text_delta", ["text"] = Text(block["text"]) ?? string.Empty };
            }

            events.Add(("content_block_start", new JsonObject { ["type"] = "content_block_start", ["index"] = index, ["content_block"] = empty }.ToJsonString()));
            events.Add(("content_block_delta", new JsonObject { ["type"] = "content_block_delta", ["index"] = index, ["delta"] = delta }.ToJsonString()));
            events.Add(("content_block_stop", new JsonObject { ["type"] = "content_block_stop", ["index"] = index }.ToJsonString()));
            index++;
        }

        events.Add(("message_delta", new JsonObject
        {
            ["type"] = "message_delta",
            ["delta"] = new JsonObject { ["stop_reason"] = message["stop_reason"]?.DeepClone(), ["stop_sequence"] = null },
            ["usage"] = new JsonObject { ["output_tokens"] = message["usage"]?["output_tokens"]?.DeepClone() }
        }.ToJsonString()));
        events.Add(("message_stop", new JsonObject { ["type"] = "message_stop" }.ToJsonString()));
        return events;
    }

    static JsonObject Request(string orderId, JsonArray messages, JsonArray tools, JsonNode? maxTokens, JsonNode? temperature)
    {
        var request = new JsonObject { ["orderId"] = orderId, ["messages"] = messages };
        if (tools.Count > 0) request["tools"] = tools;
        if (maxTokens is JsonValue max && max.TryGetValue<int>(out var tokens) && tokens > 0) request["maxOutputTokens"] = Math.Min(tokens, 32768);
        if (temperature is JsonValue temp && temp.TryGetValue<double>(out var degrees)) request["temperature"] = Math.Clamp(degrees, 0, 2);
        return request;
    }

    static string OpenAiFinish(string? finish) => finish switch { "toolCalls" => "tool_calls", "length" => "length", _ => "stop" };

    static long Tokens(JsonObject answer, string name) => answer["usage"]?[name] is JsonValue value && value.TryGetValue<long>(out var count) ? count : 0;

    static JsonObject ParseArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments)) return [];
        try
        {
            return JsonNode.Parse(arguments) as JsonObject ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    static string? ContentText(JsonNode? content) => content switch
    {
        JsonValue value when value.TryGetValue<string>(out var text) => text,
        JsonArray parts => string.Join('\n', parts.OfType<JsonObject>().Select(part => Text(part["text"])).OfType<string>()),
        _ => null
    };

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
