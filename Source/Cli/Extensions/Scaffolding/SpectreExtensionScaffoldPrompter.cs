// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// Asks the wizard's questions in the terminal.
/// </summary>
public class SpectreExtensionScaffoldPrompter : IExtensionScaffoldPrompter
{
    /// <inheritdoc/>
    public string Choose(string question, IReadOnlyList<string> choices) =>
        AnsiConsole.Prompt(new SelectionPrompt<string>().Title(question.EscapeMarkup()).AddChoices(choices));

    /// <inheritdoc/>
    public string Ask(string question, string? suggestion)
    {
        var prompt = new TextPrompt<string>(question.EscapeMarkup());
        if (suggestion is not null) prompt.DefaultValue(suggestion).ShowDefaultValue(true);
        return AnsiConsole.Prompt(prompt);
    }
}
