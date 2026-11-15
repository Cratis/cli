// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// A slice an order targets.
/// </summary>
/// <param name="Id">The stable identifier of the slice.</param>
/// <param name="Name">The name of the slice, as written in the model.</param>
/// <param name="Type">The slice type, by its wire name.</param>
public sealed record BuildOrderSlice(string Id, string Name, string Type);

/// <summary>
/// A Screenplay document carried by an order.
/// </summary>
/// <param name="Path">The document's path, relative to the model root.</param>
/// <param name="Content">The document's text.</param>
public sealed record BuildOrderDocument(string Path, string Content);

/// <summary>
/// Feedback a person left on a slice an order targets.
/// </summary>
/// <param name="SliceId">The slice the feedback is on.</param>
/// <param name="Text">What the person wrote.</param>
public sealed record BuildOrderFeedback(string SliceId, string Text);

/// <summary>
/// A specification of a slice an order targets: an acceptance criterion the build must meet.
/// </summary>
/// <param name="SliceId">The slice it specifies.</param>
/// <param name="Id">The specification's identifier, which the outcome reports results by.</param>
/// <param name="Name">The specification's name.</param>
public sealed record BuildOrderSpecification(string SliceId, string Id, string Name);

/// <summary>
/// A build order from Studio, in contract version 1 or later: what to build, described in Screenplay. The document is
/// kept as it arrived, so a builder hands the agent exactly what Studio wrote.
/// </summary>
/// <param name="Document">The order as Studio wrote it.</param>
public sealed record BuildOrder(JsonObject Document)
{
    /// <summary>
    /// Gets the contract version the order is written in.
    /// </summary>
    public string ContractVersion => Text(Document["contractVersion"]) ?? "1";

    /// <summary>
    /// Gets the identifier of the order.
    /// </summary>
    public string OrderId => Text(Document["orderId"]) ?? string.Empty;

    /// <summary>
    /// Gets the part the order builds: backend or ui.
    /// </summary>
    public string Part => Text(Document["part"]) ?? "backend";

    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string Application => Text(Document["scope"]?["application"]?["name"]) ?? string.Empty;

    /// <summary>
    /// Gets the name of the feature the slices belong to.
    /// </summary>
    public string Feature => Text(Document["scope"]?["feature"]?["name"]) ?? string.Empty;

    /// <summary>
    /// Gets the modules and parent features down to the feature.
    /// </summary>
    public IReadOnlyList<string> FeaturePath =>
        [.. Strings(Document["scope"]?["feature"]?["path"]), .. new[] { Feature }.Where(name => name.Length > 0)];

    /// <summary>
    /// Gets the target slices.
    /// </summary>
    public IReadOnlyList<BuildOrderSlice> Slices =>
        [.. (Document["scope"]?["slices"] as JsonArray ?? []).OfType<JsonObject>().Select(slice =>
            new BuildOrderSlice(Text(slice["id"]) ?? string.Empty, Text(slice["name"]) ?? string.Empty, Text(slice["type"]) ?? string.Empty))];

    /// <summary>
    /// Gets the attempt number, starting at 1.
    /// </summary>
    public int Attempt => Document["context"]?["attempt"] is JsonValue value && value.TryGetValue<int>(out var attempt) && attempt > 0 ? attempt : 1;

    /// <summary>
    /// Gets the Screenplay documents of the order.
    /// </summary>
    public IReadOnlyList<BuildOrderDocument> Documents =>
        [.. (Document["model"]?["documents"] as JsonArray ?? []).OfType<JsonObject>().Select(document =>
            new BuildOrderDocument(Text(document["path"]) ?? string.Empty, Text(document["content"]) ?? string.Empty))];

    /// <summary>
    /// Gets the hints of the order: guidance for the build, among them each target slice's description of what it is supposed to do.
    /// </summary>
    public IReadOnlyList<string> Hints => [.. Strings(Document["context"]?["hints"]).Where(hint => !string.IsNullOrWhiteSpace(hint))];

    /// <summary>
    /// Gets the feedback people left on the target slices.
    /// </summary>
    public IReadOnlyList<BuildOrderFeedback> Feedback =>
        [.. (Document["context"]?["feedback"] as JsonArray ?? []).OfType<JsonObject>()
            .Select(feedback => new BuildOrderFeedback(Text(feedback["sliceId"]) ?? string.Empty, Text(feedback["text"]) ?? string.Empty))
            .Where(feedback => !string.IsNullOrWhiteSpace(feedback.Text))];

    /// <summary>
    /// Gets the specifications of the target slices, each an acceptance criterion.
    /// </summary>
    public IReadOnlyList<BuildOrderSpecification> Specifications =>
        [.. (Document["specifications"] as JsonArray ?? []).OfType<JsonObject>().Select(specification => new BuildOrderSpecification(
            Text(specification["sliceId"]) ?? string.Empty,
            Text(specification["id"]) ?? string.Empty,
            Text(specification["name"]) ?? string.Empty))];

    /// <summary>
    /// Reads an order from its JSON text.
    /// </summary>
    /// <param name="json">The order's JSON.</param>
    /// <returns>The order.</returns>
    /// <exception cref="InvalidBuildOrder">The text is not an order.</exception>
    public static BuildOrder Parse(string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidBuildOrder($"The order is not JSON: {exception.Message}");
        }

        return From(node);
    }

    /// <summary>
    /// Reads an order from a parsed JSON node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The order.</returns>
    /// <exception cref="InvalidBuildOrder">The node is not an order.</exception>
    public static BuildOrder From(JsonNode? node)
    {
        if (node is not JsonObject document) throw new InvalidBuildOrder("The order is not a JSON object.");
        var order = new BuildOrder(document);
        if (order.OrderId.Length == 0) throw new InvalidBuildOrder("The order has no orderId.");
        if (order.Slices.Count == 0) throw new InvalidBuildOrder("The order targets no slices.");
        return order;
    }

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    static IEnumerable<string> Strings(JsonNode? node) =>
        (node as JsonArray ?? []).Select(Text).OfType<string>();
}
