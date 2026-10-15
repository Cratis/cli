// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Summarizes runtime issues without hiding any admission failure.
/// </summary>
internal static partial class StageRefusalGroups
{
    internal const string FixYourModel = "Fix your model";
    internal const string NotSupported = "Not supported by this engine yet";
    internal const string Ignored = "Ignored by this engine";

    [GeneratedRegex(@"^(?:(?<location>SourceLocation\s*\{[^}]*\})\s*:?\s*|(?<location>(?:[A-Za-z]:)?[^:\r\n]+?\(\d+,\s*\d+\)):\s*)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    static partial Regex LocationPrefix { get; }

    [GeneratedRegex("'[^']*'", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    static partial Regex QuotedName { get; }

    internal static IReadOnlyList<StageRefusalGroup> Create(IReadOnlyList<StageRuntimeIssue> issues) =>
    [
        .. issues
        .Select(Normalize)
        .GroupBy(issue => issue.Kind, StringComparer.Ordinal)
        .Select(group => new StageRefusalGroup(Category(group.Key), group.Key, group.Count(), [.. group.Take(3).Select(issue => issue.Location)]))
        .OrderBy(group => CategoryOrder(group.Category))
        .ThenByDescending(group => group.Count)
        .ThenBy(group => group.Kind, StringComparer.Ordinal)
    ];

    internal static string Category(string kind)
    {
        if (ContainsAny(kind, "is missing required property", "Consider drawing the producer before the consumer", "depend on each other's events or read models"))
        {
            return FixYourModel;
        }

        return ContainsAny(kind, "authoring metadata", "realization metadata", "operational metadata", "explicitly deferred") ? Ignored : NotSupported;
    }

    static (string Kind, string Location) Normalize(StageRuntimeIssue issue)
    {
        var location = LocationPrefix.Match(issue.Details);
        var text = location.Success ? issue.Details[location.Length..] : issue.Details;

        return (QuotedName.Replace(text, "'…'"), location.Success ? location.Groups["location"].Value : issue.Artifact);
    }

    static bool ContainsAny(string text, params string[] phrases) => phrases.Any(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase));

    static int CategoryOrder(string category) => category switch
    {
        FixYourModel => 0,
        NotSupported => 1,
        _ => 2
    };

}

/// <summary>
/// One normalized refusal kind and its first three source locations.
/// </summary>
/// <param name="Category">The model-fix, engine-gap or metadata category.</param>
/// <param name="Kind">The refusal text with source prefixes and quoted names normalized.</param>
/// <param name="Count">The number of original issues in this group.</param>
/// <param name="Locations">Up to three original source prefixes, or artifact names.</param>
internal sealed record StageRefusalGroup(string Category, string Kind, int Count, IReadOnlyList<string> Locations)
{
    internal string Summary => $"{Count} × {Kind} at {string.Join(", ", Locations)}{(Count > Locations.Count ? $" (+{Count - Locations.Count} more)" : string.Empty)}";
}
