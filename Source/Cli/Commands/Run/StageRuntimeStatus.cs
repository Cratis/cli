// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The runtime admission state reported by the Stage, independent of whether its HTTP server is listening.
/// </summary>
/// <param name="State">The warm, loading, ready or unsupported state.</param>
/// <param name="Engine">The engine actually running, when reported by the image.</param>
/// <param name="Issues">The reasons the model cannot run, when reported.</param>
public sealed record StageRuntimeStatus(string State, string? Engine = null, IReadOnlyList<StageRuntimeIssue>? Issues = null)
{
    /// <summary>
    /// Gets non-blocking compiler diagnostics reported by newer Stage images.
    /// </summary>
    public IReadOnlyList<StageRuntimeNote>? Notes { get; init; }

    /// <summary>
    /// Gets a value indicating whether runtime admission has refused the model.
    /// </summary>
    public bool IsUnsupported => string.Equals(State, "unsupported", StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether the host is still waiting for or loading a model.
    /// </summary>
    public bool IsPending => string.Equals(State, "warm", StringComparison.Ordinal) || string.Equals(State, "loading", StringComparison.Ordinal);

    /// <summary>
    /// Gets the suggested next step for a refused model.
    /// </summary>
    public string Suggestion => SuggestionFor(new RunSettings());

    /// <summary>
    /// Builds the retry command without losing the user's selected input or options.
    /// </summary>
    /// <param name="settings">The original run settings.</param>
    /// <returns>The exact semantic-engine command, or the generic refusal suggestion.</returns>
    public string SuggestionFor(RunSettings settings) => string.Equals(Engine, "eventmodel", StringComparison.Ordinal) &&
        Issues?.Any(issue => issue.PointsToSemantic) == true
            ? $"Run again on the semantic engine: {settings.SemanticCommand()}"
            : "Resolve the reported unsupported capabilities or select an engine that supports the model";

    /// <summary>
    /// Determines whether an unselected engine can switch once to handle every refusal.
    /// </summary>
    /// <param name="requestedEngine">The explicitly selected engine, or null when omitted.</param>
    /// <returns>True only for an implicit EventModel engine whose nonempty refusal set all points to semantic.</returns>
    public bool ShouldSwitchToSemantic(string? requestedEngine) => requestedEngine is null && IsUnsupported &&
        string.Equals(Engine, "eventmodel", StringComparison.Ordinal) &&
        Issues is { Count: > 0 } && Issues.All(issue => issue.PointsToSemantic);

    /// <summary>
    /// Reads the Stage status contract, falling back for older images without a usable status body.
    /// </summary>
    /// <param name="body">The response body.</param>
    /// <returns>The recognized runtime status, or null for the legacy readiness behavior.</returns>
    public static StageRuntimeStatus? Read(string body)
    {
        try
        {
            var status = JsonSerializer.Deserialize<StageRuntimeStatus>(body, JsonSerializerOptions.Web);
            return status is not null && (status.IsPending || status.IsUnsupported || string.Equals(status.State, "ready", StringComparison.Ordinal)) &&
                status.Issues?.Any(issue => issue is null || issue.Capability is null || issue.Artifact is null || issue.Details is null) != true &&
                status.Notes?.Any(note => note is null || note.Code is null || note.Message is null) != true
                    ? status
                    : null;
        }
        catch (JsonException)
        {
            // Older images can serve HTML or an unrelated response at this route instead of the status contract.
            return null;
        }
    }
}
