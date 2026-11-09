// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Composes build outcomes in the shape the contract declares. An agent writes what it built, why it is blocked or that
/// nothing needed building; the host keeps only the members the contract declares for that kind, so a stray member never
/// makes Studio refuse the outcome, and the host alone adds the Git members.
/// </summary>
public static class BuildOutcomes
{
    /// <summary>The kind of an outcome whose slices were built.</summary>
    public const string Built = "built";

    /// <summary>The kind of an outcome whose slices cannot be built until someone answers.</summary>
    public const string Blocked = "blocked";

    /// <summary>The kind of an outcome whose attempt went wrong.</summary>
    public const string Failed = "failed";

    /// <summary>The kind of an outcome where nothing needed building.</summary>
    public const string NothingToDo = "nothingToDo";

    static readonly string[] _specificationResults = ["passed", "failed", "notRun"];

    /// <summary>
    /// Creates a failed outcome.
    /// </summary>
    /// <param name="order">The order it answers.</param>
    /// <param name="error">What went wrong.</param>
    /// <param name="retryable">Whether trying again can succeed.</param>
    /// <returns>The outcome.</returns>
    public static JsonObject Failure(BuildOrder order, string error, bool retryable) => new()
    {
        ["contractVersion"] = "1",
        ["orderId"] = order.OrderId,
        ["kind"] = Failed,
        ["error"] = string.IsNullOrWhiteSpace(error) ? "The build failed." : error,
        ["retryable"] = retryable
    };

    /// <summary>
    /// Reads the outcome an agent or a generator wrote, keeping only what the contract declares for its kind.
    /// </summary>
    /// <param name="order">The order it answers.</param>
    /// <param name="json">The text the agent wrote, or <see langword="null"/> when it wrote nothing.</param>
    /// <returns>The outcome, or a retryable failure saying why the agent's document cannot be used.</returns>
    public static JsonObject FromAgent(BuildOrder order, string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Failure(order, "The agent finished without writing an outcome.", true);

        JsonObject written;
        try
        {
            if (JsonNode.Parse(json) is not JsonObject parsed) return Failure(order, "The agent's outcome is not a JSON object.", true);
            written = parsed;
        }
        catch (JsonException exception)
        {
            return Failure(order, $"The agent's outcome is not JSON: {exception.Message}", true);
        }

        var kind = Text(written["kind"]);
        return kind switch
        {
            Built => BuiltFrom(order, written),
            Blocked => BlockedFrom(order, written),
            NothingToDo => Head(order, NothingToDo),
            Failed => Failure(order, Text(written["error"]) ?? "The agent reported a failure.", written["retryable"] is not JsonValue retry || !retry.TryGetValue<bool>(out var retryable) || retryable),
            _ => Failure(order, $"The agent's outcome has the kind '{kind}', which is not built, blocked, failed or nothingToDo.", true)
        };
    }

    static JsonObject BuiltFrom(BuildOrder order, JsonObject written)
    {
        var outcome = Head(order, Built);
        outcome["files"] = new JsonArray([.. Texts(written["files"]).Select(file => (JsonNode)file)]);
        outcome["specifications"] = new JsonArray([.. (written["specifications"] as JsonArray ?? []).OfType<JsonObject>()
            .Where(result => Text(result["id"]) is { Length: > 0 } && _specificationResults.Contains(Text(result["result"])))
            .Select(result =>
            {
                var kept = new JsonObject { ["id"] = Text(result["id"]), ["result"] = Text(result["result"]) };
                if (Text(result["message"]) is { Length: > 0 } message) kept["message"] = message;
                return (JsonNode)kept;
            })]);
        outcome["assumptions"] = new JsonArray([.. Texts(written["assumptions"]).Select(assumption => (JsonNode)assumption)]);
        return outcome;
    }

    static JsonObject BlockedFrom(BuildOrder order, JsonObject written)
    {
        var outcome = Head(order, Blocked);
        outcome["reason"] = Text(written["reason"]) is { Length: > 0 } reason ? reason : "The agent could not build the slices and gave no reason.";
        outcome["questions"] = new JsonArray([.. Texts(written["questions"]).Select(question => (JsonNode)question)]);
        return outcome;
    }

    static JsonObject Head(BuildOrder order, string kind) => new()
    {
        ["contractVersion"] = "1",
        ["orderId"] = order.OrderId,
        ["kind"] = kind
    };

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text) ? text : null;

    static IEnumerable<string> Texts(JsonNode? node) => (node as JsonArray ?? []).Select(Text).OfType<string>();
}
