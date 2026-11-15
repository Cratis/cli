// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Removes credentials from what the builder writes to its log and reports to Studio: every credential the host itself
/// holds, by value, and anything that looks like one (bearer tokens, well-known token prefixes, private keys and
/// credentials in URLs).
/// </summary>
/// <param name="known">The credentials the host holds.</param>
public sealed partial class BuildRedaction(IEnumerable<string?> known)
{
    /// <summary>What a removed credential is replaced with.</summary>
    public const string Mask = "[redacted]";

    readonly string[] _known = [.. known.OfType<string>().Where(secret => secret.Length >= 8).Distinct(StringComparer.Ordinal).OrderByDescending(secret => secret.Length)];

    /// <summary>
    /// Gets a redaction that only removes what looks like a credential.
    /// </summary>
    public static BuildRedaction Patterns { get; } = new([]);

    [GeneratedRegex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----[\s\S]*?(-----END [A-Z ]*PRIVATE KEY-----|$)", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex PrivateKey { get; }

    [GeneratedRegex(@"(?i)(?<prefix>\bbearer\s+)[A-Za-z0-9._~+/=-]{8,}", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Bearer { get; }

    [GeneratedRegex(@"(?<prefix>[a-z][a-z0-9+.-]*://[^\s:/@]+:)[^\s@/]+@", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex UrlCredential { get; }

    [GeneratedRegex(@"\b(gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|sk-[A-Za-z0-9_-]{20,}|xox[abprs]-[A-Za-z0-9-]{10,}|AKIA[0-9A-Z]{16})\b", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex TokenLike { get; }

    /// <summary>
    /// Removes credentials from a text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The text without credentials.</returns>
    public string Redact(string text)
    {
        var redacted = _known.Aggregate(text, (current, secret) => current.Replace(secret, Mask, StringComparison.Ordinal));
        redacted = PrivateKey.Replace(redacted, Mask);
        redacted = Bearer.Replace(redacted, match => $"{match.Groups["prefix"].Value}{Mask}");
        redacted = UrlCredential.Replace(redacted, match => $"{match.Groups["prefix"].Value}{Mask}@");
        return TokenLike.Replace(redacted, Mask);
    }

    /// <summary>
    /// Removes credentials from every text in an outcome.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The same outcome, with every string redacted.</returns>
    public JsonObject Redact(JsonObject outcome)
    {
        foreach (var name in outcome.Select(pair => pair.Key).ToList())
        {
            outcome[name] = Redact(outcome[name]);
        }

        return outcome;
    }

    JsonNode? Redact(JsonNode? node) => node switch
    {
        JsonObject inner => Redact(inner),
        JsonArray array => new JsonArray([.. array.Select(item => Redact(item?.DeepClone()))]),
        JsonValue value when value.TryGetValue<string>(out var text) => JsonValue.Create(Redact(text)),
        null => null,
        _ => node.DeepClone()
    };
}
