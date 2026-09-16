// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.Configuration;

namespace Cratis.Cli.Templates;

/// <summary>
/// Interactive prompting for parameter symbols carrying <c language="csharp">prompt</c>, shown only when attached to a
/// terminal. Every prompt has a non-interactive equivalent: the parameter flag itself, and
/// <c language="csharp">--no-prompts</c> to always take defaults.
/// </summary>
public static class TemplateParameterPrompts
{
    /// <summary>
    /// Prompts for all promptable parameters that the binding did not already provide a value for.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="bound">Values already bound from the command line.</param>
    /// <param name="interactive">Whether an interactive terminal is attached.</param>
    /// <returns>The bound values plus prompted values.</returns>
    public static IReadOnlyDictionary<string, string> PromptForMissing(
        TemplateConfig manifest,
        IReadOnlyDictionary<string, string> bound,
        bool interactive)
    {
        var values = new Dictionary<string, string>(bound, StringComparer.Ordinal);
        foreach (var symbol in manifest.Symbols.Values.Where(symbol => symbol.Type == SymbolType.Parameter && symbol.Prompt is not null))
        {
            if (values.ContainsKey(symbol.Name))
            {
                continue;
            }

            if (!interactive)
            {
                continue;
            }

            values[symbol.Name] = symbol.DataType == "choice"
                ? PromptChoice(symbol)
                : PromptText(symbol);
        }
        return values;
    }

    static string PromptChoice(SymbolConfig symbol)
    {
        var choices = symbol.Choices.Select(choice => choice.Choice).ToArray();
        var selection = new SelectionPrompt<string>()
            .Title(symbol.Prompt ?? symbol.DisplayName ?? symbol.Name)
            .PageSize(Math.Min(choices.Length, 12))
            .HighlightStyle(new Style(OutputFormatter.Accent))
            .AddChoices(choices);
        var picked = AnsiConsole.Prompt(selection);
        return choices.Contains(picked) ? picked : symbol.DefaultValue ?? picked;
    }

    static string PromptText(SymbolConfig symbol)
    {
        if (string.Equals(symbol.DataType, "bool", StringComparison.Ordinal) || string.Equals(symbol.DataType, "bool?", StringComparison.Ordinal))
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(symbol.Prompt ?? symbol.Name)
                    .AddChoices(["true", "false"]));
        }

        var prompt = new TextPrompt<string>(symbol.Prompt ?? symbol.DisplayName ?? symbol.Name);
        if (symbol.DefaultValue is not null)
        {
            prompt = prompt.DefaultValue(symbol.DefaultValue);
        }
        return AnsiConsole.Prompt(prompt);
    }
}
