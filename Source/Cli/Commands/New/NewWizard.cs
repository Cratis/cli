// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;
using Cratis.Templating;
using Spectre.Console;

namespace Cratis.Cli.Commands.New;

/// <summary>
/// The interactive creation wizard behind bare <c language="csharp">cratis new</c>: which template (defaulting to the
/// <c language="csharp">cratis</c> concept), then which language, then which database — every question offering only
/// what the chosen template supports, and every single-choice question skipped because its choice
/// is already made. Non-interactive terminals get guidance instead of a hang.
/// </summary>
public static class NewWizard
{
    /// <summary>
    /// Runs the three questions and returns the chosen combination, or null when the terminal is
    /// not interactive (reported) or a choice was cancelled.
    /// </summary>
    /// <param name="concepts">The concept list the questions operate over.</param>
    /// <param name="interactive">Whether an interactive terminal is attached.</param>
    /// <returns>The chosen template, its concept, language, and database — or null.</returns>
    public static (Cratis.Templating.Packages.DiscoveredTemplate Template, ConceptTemplate Concept, string Language, string? Database)? Ask(
        IReadOnlyList<ConceptTemplate> concepts,
        bool interactive)
    {
        if (!interactive)
        {
            Report("cratis new needs a template", "run 'cratis new list' to see the available templates, or pass one: cratis new <template> --language csharp -n <Name>.");
            return null;
        }

        // Question 1 — which template; the cratis concept is the default and lands first in the
        // prompt, which the terminal highlights.
        var ordered = concepts.OrderByDescending(concept => concept.ShortName == "cratis").ThenBy(concept => concept.ShortName).ToArray();
        var concept = AnsiConsole.Prompt(new SelectionPrompt<ConceptTemplate>()
            .Title("Which template?")
            .PageSize(Math.Min(ordered.Length, 10))
            .HighlightStyle(new Style(OutputFormatter.Accent))
            .UseConverter(concept => $"{concept.ShortName} — {concept.Name}")
            .AddChoices(ordered));

        // Question 2 — which language; a single-language concept is already decided.
        var language = concept.Languages.Count > 1
            ? AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Which language?")
                .HighlightStyle(new Style(OutputFormatter.Accent))
                .AddChoices(concept.Languages))
            : concept.DefaultLanguage;

        var member = concept.MemberFor(language);
        if (member is null)
        {
            Report("template not found", $"the {concept.ShortName} concept does not offer language '{language}'.");
            return null;
        }

        // Question 3 — which database; a single-database template is already decided. A template
        // without a Database parameter keeps its implicit default — MongoDB in the catalogue.
        var databases = concept.DatabasesFor(language);
        if (databases.Count == 0)
        {
            return (member, concept, language, null);
        }

        if (databases.Count == 1)
        {
            return (member, concept, language, databases[0]);
        }

        var database = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Which database?")
            .HighlightStyle(new Style(OutputFormatter.Accent))
            .AddChoices(databases));

        return (member, concept, language, database);
    }

    static void Report(string headline, string detail)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Danger.ToMarkup()}]✗ {headline.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"  {detail.EscapeMarkup()}");
        AnsiConsole.WriteLine();
    }
}
