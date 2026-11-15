// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;
using Cratis.Templating;
using Cratis.Templating.PostActions;

namespace Cratis.Cli.Commands.New;

/// <summary>
/// Reports what <see cref="NewCommand"/> created: as JSON, or as a summary line, one line per post action and the Cratis AI update.
/// </summary>
internal static class NewCreationOutput
{
    /// <summary>
    /// Renders the creation in the format the settings ask for.
    /// </summary>
    /// <param name="settings">The command settings — format and dry run apply.</param>
    /// <param name="creation">What was created.</param>
    public static void Render(NewSettings settings, NewCreation creation)
    {
        if (settings.Format == "json")
        {
            WriteJson(settings, creation);
            return;
        }

        WriteSummary(settings.DryRun, creation.Result);
        WriteActions(creation.Actions);
        WriteAiUpdate(creation.AiUpdate);
        AnsiConsole.WriteLine();
    }

    static void WriteJson(NewSettings settings, NewCreation creation)
    {
        var result = creation.Result;
        OutputFormatter.WriteObject(OutputFormats.Json, new
        {
            template = creation.Template.Manifest.ShortName,
            name = result.Name,
            output = result.OutputRoot,
            dryRun = settings.DryRun,
            files = result.CreatedFiles.Select(file => RelativeTo(result, file.Path)),
            primaryOutputs = result.PrimaryOutputs.Select(path => RelativeTo(result, path)),
            postActions = creation.Actions.Select(action => new
            {
                description = Describe(action),
                outcome = action.Outcome.ToString().ToLowerInvariant(),
                message = action.Message,
                instructions = action.Instructions.Length > 0 ? action.Instructions : null,
                continueOnError = action.Action.ContinueOnError
            }),
            aiUpdate = new
            {
                status = creation.AiUpdate.Status,
                detail = creation.AiUpdate.Detail,
                actions = creation.AiUpdate.Actions
            }
        });
    }

    static string RelativeTo(InstantiationResult result, string path) =>
        Path.GetRelativePath(result.OutputRoot, path).Replace('\\', '/');

    static string Describe(PostActionResult action) => action.Action.Description ?? action.Action.ActionId;

    static void WriteSummary(bool dryRun, InstantiationResult result)
    {
        var muted = OutputFormatter.Muted.ToMarkup();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(dryRun
            ? $"  [{OutputFormatter.Warning.ToMarkup()}]Dry run[/] [{muted}]— nothing was written. {result.CreatedFiles.Count} file(s) would be created at[/] [bold]{result.OutputRoot.EscapeMarkup()}[/]"
            : $"  [{OutputFormatter.Success.ToMarkup()}]✓[/] Created [bold]{result.Name.EscapeMarkup()}[/] [{muted}]at[/] [bold]{result.OutputRoot.EscapeMarkup()}[/]");
    }

    static void WriteActions(IReadOnlyList<PostActionResult> actions)
    {
        if (actions.Count == 0)
        {
            return;
        }

        AnsiConsole.WriteLine();
        foreach (var action in actions)
        {
            WriteAction(action);
        }
    }

    static void WriteAction(PostActionResult action)
    {
        var muted = OutputFormatter.Muted.ToMarkup();
        var (symbol, color) = MarkerFor(action.Outcome);
        AnsiConsole.MarkupLine($"  [{color}]{symbol}[/] {Describe(action).EscapeMarkup()}");
        if (action.Message.Length > 0 && action.Outcome is not (PostActionOutcome.Succeeded or PostActionOutcome.Skipped))
        {
            AnsiConsole.MarkupLine($"      [{muted}]{action.Message.EscapeMarkup()}[/]");
        }
        if (action.Instructions.Length > 0)
        {
            AnsiConsole.MarkupLine($"      [{muted}]{action.Instructions.EscapeMarkup()}[/]");
        }
    }

    static (char Symbol, string Color) MarkerFor(PostActionOutcome outcome) => outcome switch
    {
        PostActionOutcome.Succeeded or PostActionOutcome.Displayed => ('✓', OutputFormatter.Success.ToMarkup()),
        PostActionOutcome.Skipped => ('·', OutputFormatter.Muted.ToMarkup()),
        PostActionOutcome.NotPerformed => ('·', OutputFormatter.Warning.ToMarkup()),
        _ => ('✗', OutputFormatter.Warning.ToMarkup())
    };

    static void WriteAiUpdate(CreatedProjectAiUpdateResult aiUpdate)
    {
        var color = aiUpdate.Status switch
        {
            "updated" => OutputFormatter.Success.ToMarkup(),
            "skipped" => OutputFormatter.Muted.ToMarkup(),
            _ => OutputFormatter.Warning.ToMarkup()
        };
        var symbol = aiUpdate.Status == "updated" ? "✓" : "·";
        AnsiConsole.MarkupLine($"  [{color}]{symbol}[/] Cratis AI: {aiUpdate.Detail.EscapeMarkup()}");
    }
}
