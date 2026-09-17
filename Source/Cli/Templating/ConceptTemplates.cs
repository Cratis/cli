// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.Packages;

namespace Cratis.Cli.Templates;

/// <summary>
/// One concept template: a template, or a family of language-specific derivatives sharing a
/// <c language="csharp">groupIdentity</c>. Languages come from each member's <c language="csharp">tags.language</c>; databases from the
/// selected member's <c language="csharp">Database</c> parameter choices. A concept with a single language or a single
/// database has those choices done — no question to ask.
/// </summary>
/// <param name="Name">The concept name, from the default member.</param>
/// <param name="ShortName">The concept short name, from the default member.</param>
/// <param name="Description">The concept description, from the default member.</param>
/// <param name="DefaultLanguage">The canonical language of the default member.</param>
/// <param name="Members">The language-specific derivatives keyed by canonical language.</param>
public record ConceptTemplate(
    string Name,
    string ShortName,
    string? Description,
    string DefaultLanguage,
    IReadOnlyDictionary<string, DiscoveredTemplate> Members)
{
    /// <summary>
    /// Gets the languages the concept is available in, in canonical form.
    /// </summary>
    public IReadOnlyList<string> Languages => [.. Members.Keys.Order(StringComparer.Ordinal)];

    /// <summary>
    /// Gets the databases the selected member offers, or an empty list when the member has no
    /// Database parameter.
    /// </summary>
    /// <param name="language">The canonical language selecting the member.</param>
    /// <returns>The offered database choices.</returns>
    public IReadOnlyList<string> DatabasesFor(string language) =>
        MemberFor(language)?.Manifest.Symbols.TryGetValue("Database", out var database) == true
            ? [.. database.Choices.Select(choice => choice.Choice)]
            : [];

    /// <summary>
    /// Gets the member for a language, falling back to the default member.
    /// </summary>
    /// <param name="language">The canonical language.</param>
    /// <returns>The member template, or null when the language is not offered.</returns>
    public DiscoveredTemplate? MemberFor(string language) =>
        Members.TryGetValue(language, out var member) ? member : null;
}

/// <summary>
/// Builds the concept list from discovered templates: templates sharing a <c language="csharp">groupIdentity</c>
/// collapse into one concept keyed by canonical language; standalone templates are their own
/// concept. The default member is the highest-precedence member — the group's display and
/// fallback member, exactly like the upstream engine's collapse.
/// </summary>
public static class ConceptTemplates
{
    /// <summary>
    /// Builds the concept list.
    /// </summary>
    /// <param name="templates">Every template discovered across the acquired packages.</param>
    /// <returns>The concepts ordered by short name.</returns>
    public static IReadOnlyList<ConceptTemplate> Build(IReadOnlyList<DiscoveredTemplate> templates)
    {
        var groups = new Dictionary<string, List<DiscoveredTemplate>>(StringComparer.Ordinal);
        foreach (var template in templates)
        {
            var key = template.Manifest.GroupIdentity ?? template.Manifest.Identity ?? template.Manifest.ShortName;
            if (!groups.TryGetValue(key, out var members))
            {
                groups[key] = members = [];
            }
            members.Add(template);
        }

        var concepts = new List<ConceptTemplate>();
        foreach (var members in groups.Values)
        {
            var defaultMember = members
                .OrderByDescending(member => member.Manifest.Precedence)
                .ThenBy(member => LanguageOf(member) == "csharp" ? 0 : 1)
                .First();

            var languageMembers = new Dictionary<string, DiscoveredTemplate>(StringComparer.Ordinal);
            foreach (var member in members)
            {
                languageMembers[LanguageOf(member)] = member;
            }

            concepts.Add(new ConceptTemplate(
                defaultMember.Manifest.Name,
                defaultMember.Manifest.ShortName,
                defaultMember.Manifest.Description,
                LanguageOf(defaultMember),
                languageMembers));
        }
        return [.. concepts.OrderBy(concept => concept.ShortName, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Finds a concept by its short name or a member's identity, case-insensitively.
    /// </summary>
    /// <param name="concepts">The concept list.</param>
    /// <param name="name">The concept short name or member identity.</param>
    /// <returns>The concept, or null.</returns>
    public static ConceptTemplate? Find(IReadOnlyList<ConceptTemplate> concepts, string name) =>
        concepts.FirstOrDefault(concept => concept.ShortName.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? concepts.FirstOrDefault(concept => concept.Members.Values.Any(member =>
            member.Manifest.Identity?.Equals(name, StringComparison.OrdinalIgnoreCase) == true));

    static string LanguageOf(DiscoveredTemplate template)
    {
        if (template.Manifest.Tags.TryGetValue("language", out var language))
        {
            var normalized = LanguageSelection.Normalize(language);
            if (normalized is not null)
            {
                return normalized;
            }
        }
        return "csharp";
    }
}
