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

    internal static IReadOnlyList<StageRefusalGroup> Create(IReadOnlyList<StageRuntimeIssue> issues, IReadOnlyList<StageRuntimeNote>? notes = null) =>
    [
        .. issues
        .Select(issue => Normalize(issue.Details, issue.Artifact))
        .Select(issue => (Category: Category(issue.Kind), issue.Kind, issue.Location))
        .Concat((notes ?? []).Select(Normalize))
        .GroupBy(issue => (issue.Category, issue.Kind))
        .Select(group => new StageRefusalGroup(group.Key.Category, group.Key.Kind, group.Count(), [.. group.Take(3).Select(issue => issue.Location)]))
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

    static (string Category, string Kind, string Location) Normalize(StageRuntimeNote note)
    {
        var normalized = Normalize(note.Message, note.Source ?? "model");

        return (Ignored, $"{note.Code}: {normalized.Kind}", note.Source ?? normalized.Location);
    }

    static (string Kind, string Location) Normalize(string details, string artifact)
    {
        var location = LocationPrefix.Match(details);
        var text = location.Success ? details[location.Length..] : details;

        return (QuotedName.Replace(text, "'…'"), location.Success ? location.Groups["location"].Value : artifact);
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
