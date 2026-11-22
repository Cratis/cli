// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>The log levels in Grip's build-progress.2.schema.json contract.</summary>
public enum BuildProgressLevel
{
    /// <summary>Informational activity.</summary>
    Information,

    /// <summary>A warning.</summary>
    Warning,

    /// <summary>An error.</summary>
    Error
}

/// <summary>A new log line, without host-owned timestamps.</summary>
/// <param name="Text">The text.</param>
/// <param name="Level">The severity.</param>
public sealed record BuildProgressLine(string Text, BuildProgressLevel Level = BuildProgressLevel.Information);

/// <summary>Cumulative token counts for this attempt, never deltas.</summary>
/// <param name="InputTokens">Input tokens.</param>
/// <param name="OutputTokens">Output tokens.</param>
public sealed record BuildProgressUsage(long InputTokens, long OutputTokens);

/// <summary>
/// Optional build telemetry. The host owns identity, redaction and serialization according to Grip
/// 5b02c91d1d8c28b32c8b977b1471963f463a38a0, Schemas/build-progress.2.schema.json.
/// </summary>
/// <param name="Step">Current activity.</param>
/// <param name="Log">New lines, oldest first.</param>
/// <param name="Usage">Cumulative usage when known.</param>
public sealed record BuildProgress(string? Step = null, IReadOnlyList<BuildProgressLine>? Log = null, BuildProgressUsage? Usage = null)
{
    /// <summary>Whether there is anything to send.</summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Step) || Log is { Count: > 0 } || Usage is not null;

    /// <summary>Gets a value indicating whether a build contract version carries progress: contract 2 and later.</summary>
    /// <param name="version">The build contract version.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    public static bool Supports(string version) => string.Equals(version, "2", StringComparison.Ordinal) || string.Equals(version, "3", StringComparison.Ordinal);

    /// <summary>Composes a schema-shaped, credential-free contract 2 progress report.</summary>
    /// <param name="version">The order's build contract version.</param>
    /// <param name="orderId">The order's UUID.</param>
    /// <param name="redaction">The host's credential redactor.</param>
    /// <returns>The report.</returns>
    /// <exception cref="ArgumentException">The report would violate the published schema.</exception>
    public JsonObject ToJson(string version, string orderId, BuildRedaction redaction)
    {
        if (!ValidIdentity(version, orderId) || !HasContent) throw new ArgumentException("Invalid build progress identity or empty payload.");
        ValidateDetails();
        var body = new JsonObject { ["contractVersion"] = version, ["orderId"] = orderId };
        if (!string.IsNullOrWhiteSpace(Step)) body["step"] = Limit(redaction.Redact(Step));
        if (Log is { Count: > 0 })
        {
            body["log"] = new JsonArray([.. Log.Select(line => (JsonNode)new JsonObject
            {
                ["occurred"] = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                ["level"] = line.Level.ToString().ToLowerInvariant(),
                ["text"] = Limit(redaction.Redact(line.Text))
            })]);
        }
        if (Usage is { } usage) body["usage"] = new JsonObject { ["inputTokens"] = usage.InputTokens, ["outputTokens"] = usage.OutputTokens };
        return body;
    }

    /// <summary>Redacts a source credential before any transport length limits are applied.</summary>
    /// <param name="redaction">The credential redactor.</param>
    /// <returns>A credential-free copy.</returns>
    public BuildProgress Redact(BuildRedaction redaction)
    {
        ValidateDetails();
        return this with
        {
            Step = Step is null ? null : redaction.Redact(Step),
            Log = Log?.Select(line => line with { Text = redaction.Redact(line.Text) }).ToArray()
        };
    }

    void ValidateDetails()
    {
        if (Log is { Count: > 100 }) throw new ArgumentException("Build progress accepts at most 100 new log lines.");
        if (Log?.Any(line => !ValidLine(line)) == true) throw new ArgumentException("Invalid build progress log line.");
        if (Usage is { InputTokens: < 0 } or { OutputTokens: < 0 }) throw new ArgumentException("Build progress usage cannot be negative.");
    }

    static bool ValidIdentity(string version, string orderId) => Supports(version) && Guid.TryParseExact(orderId, "D", out _);

    static bool ValidLine(BuildProgressLine line) => line is { Text: not null } && Enum.IsDefined(line.Level);

    static string Limit(string text) => text.Length <= 2000 ? text : text[..2000];
}
