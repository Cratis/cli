// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;
using Cratis.Prologue.Interpretation;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Represents the <see cref="IInterpreterCallbacks"/> that surface an interpreter session in the terminal —
/// status transitions render as muted progress lines and the language model's questions become interactive
/// prompts, one at a time, always with an "other" entry for a free-text answer.
/// </summary>
/// <param name="llmOptions">The language-model options the session refines with — names the provider in the progress lines.</param>
/// <param name="showStatus">Whether status transitions are rendered; off for JSON and quiet output formats.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/> the prompts observe.</param>
public class ConsoleInterpreterCallbacks(LlmOptions llmOptions, bool showStatus, CancellationToken cancellationToken) : IInterpreterCallbacks
{
    const string OtherChoiceLabel = "Other (type your own answer)";
    InterpreterStatus? _lastStatus;

    /// <inheritdoc/>
    public async Task<InterpreterAnswer> OnQuestion(InterpreterQuestion question)
    {
        AnsiConsole.WriteLine();
        if (question.Context.Length > 0)
        {
            AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]{question.Context.EscapeMarkup()}[/]");
        }

        if (question.Choices.Count == 0)
        {
            return new(question.Id, await PromptForText(question.Prompt));
        }

        var prompt = new SelectionPrompt<string>()
            .Title($"[bold]{question.Prompt.EscapeMarkup()}[/]")
            .UseConverter(label => DisplayFor(question, label))
            .AddChoices(question.Choices.Select(choice => choice.Label))
            .AddChoices(OtherChoiceLabel);

        var selected = await AnsiConsole.PromptAsync(prompt, cancellationToken);
        return new(question.Id, selected == OtherChoiceLabel ? await PromptForText("Your answer:") : selected);
    }

    /// <inheritdoc/>
    public void OnStatusChanged(InterpreterStatus status)
    {
        if (!showStatus || status == _lastStatus)
        {
            return;
        }

        _lastStatus = status;
        if (TextFor(status) is { } text)
        {
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]{text.EscapeMarkup()}[/]");
        }
    }

    static string DisplayFor(InterpreterQuestion question, string label)
    {
        var choice = question.Choices.FirstOrDefault(candidate => candidate.Label == label);
        return choice is { Description.Length: > 0 }
            ? $"{label.EscapeMarkup()} [{OutputFormatter.Muted.ToMarkup()}]— {choice.Description.EscapeMarkup()}[/]"
            : label.EscapeMarkup();
    }

    async Task<string> PromptForText(string prompt) =>
        await AnsiConsole.PromptAsync(new TextPrompt<string>(prompt.EscapeMarkup()), cancellationToken);

    string? TextFor(InterpreterStatus status) => status switch
    {
        InterpreterStatus.ReadingCaptures => "Reading captures…",
        InterpreterStatus.AnalyzingEvidence => "Analyzing evidence…",
        InterpreterStatus.BuildingModel => "Building model…",
        InterpreterStatus.Refining => $"Refining with {llmOptions.Kind}…",
        InterpreterStatus.GeneratingScreenplay => "Generating Screenplay…",
        _ => null
    };
}
