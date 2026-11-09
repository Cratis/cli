// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// A prompt waiting for the agent on the board.
/// </summary>
/// <param name="Id">The prompt.</param>
/// <param name="Text">What the person wrote.</param>
/// <param name="State">Why it waits.</param>
public sealed record ModelingPrompt(string Id, string Text, ModelingPromptState State)
{
    /// <summary>
    /// Reads the prompts Studio answered with, oldest first, skipping any the session cannot take.
    /// </summary>
    /// <param name="data">The query's data.</param>
    /// <returns>The prompts.</returns>
    public static IReadOnlyList<ModelingPrompt> ListFrom(JsonNode? data)
    {
        var items = data as JsonArray ?? data?["items"] as JsonArray ?? data?["data"] as JsonArray ?? [];
        var prompts = new List<ModelingPrompt>();
        foreach (var item in items.OfType<JsonObject>())
        {
            var id = item["id"]?.ToString();
            var text = item["prompt"]?.ToString();
            if (id is null || text is null || StateOf(item["state"]) is not { } state) continue;
            prompts.Add(new(id, text, state));
        }

        return prompts;
    }

    static ModelingPromptState? StateOf(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<int>(out var number) && Enum.IsDefined(typeof(ModelingPromptState), number)) return (ModelingPromptState)number;
        return Enum.TryParse<ModelingPromptState>(node?.ToString(), ignoreCase: true, out var named) && Enum.IsDefined(named) ? named : null;
    }
}
