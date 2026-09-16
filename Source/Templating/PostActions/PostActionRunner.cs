// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.PostActions;

/// <summary>
/// The policy governing whether post actions may launch processes.
/// </summary>
public enum ScriptPolicy
{
    /// <summary>Scripts may run without asking.</summary>
    Allow,

    /// <summary>Scripts must not run.</summary>
    Deny,

    /// <summary>Ask the person before running; requires an interactive terminal.</summary>
    Prompt
}

/// <summary>
/// The outcome of one post action.
/// </summary>
public enum PostActionOutcome
{
    /// <summary>The action completed successfully.</summary>
    Succeeded,

    /// <summary>The action failed.</summary>
    Failed,

    /// <summary>The action could not run and was reported with manual instructions — not a failure.</summary>
    NotPerformed,

    /// <summary>The action's condition was false, so it did not run.</summary>
    Skipped,

    /// <summary>The action's actionId is not known to this engine; manual instructions are reported.</summary>
    Unknown,

    /// <summary>Instructions were displayed to the user.</summary>
    Displayed,

    /// <summary>The action was declined by the script policy.</summary>
    Denied
}

/// <summary>
/// The result of one post action.
/// </summary>
/// <param name="Action">The action configuration.</param>
/// <param name="Outcome">The outcome.</param>
/// <param name="Message">Human readable detail, empty when there is nothing to report.</param>
/// <param name="Instructions">The manual instructions to display for this action.</param>
public record PostActionResult(PostActionConfig Action, PostActionOutcome Outcome, string Message, string Instructions)
{
    /// <summary>
    /// Gets a value indicating whether the outcome counts as a failure of the run.
    /// </summary>
    public bool IsFailure => Outcome is PostActionOutcome.Failed or PostActionOutcome.Denied;
}

/// <summary>
/// Runs a template's post actions natively: project XML editing, solution writing, JSON node editing,
/// permissions, instructions, process launching and restore — with per-action <c language="csharp">continueOnError</c>,
/// unknown action ids reported as unhandled, and the toolchain-dependent restore reported rather than
/// failed when no dotnet is present.
/// </summary>
/// <param name="store">The store to use.</param>
/// <param name="scriptPolicy">The scriptPolicy to use.</param>
public class PostActionRunner(Packages.TemplatePackageStore? store = null, ScriptPolicy scriptPolicy = ScriptPolicy.Deny)
{
    /// <summary>
    /// Runs all post actions of a manifest.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="result">The instantiation result the actions operate on.</param>
    /// <param name="symbols">Resolved symbol values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One result per action, in declaration order.</returns>
    public async Task<IReadOnlyList<PostActionResult>> Run(
        TemplateConfig manifest,
        InstantiationResult result,
        IReadOnlyDictionary<string, string> symbols,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PostActionResult>();
        foreach (var action in manifest.PostActions)
        {
            if (action.Condition is not null
                && !Expressions.ExpressionEvaluator.EvaluateBoolean(
                    action.Condition, Expressions.ExpressionDialect.Cpp2, symbols))
            {
                results.Add(new PostActionResult(action, PostActionOutcome.Skipped, "condition evaluated to false", string.Empty));
                continue;
            }

            try
            {
                results.Add(await RunAction(action, result, symbols, cancellationToken));
            }
            catch (Exception error) when (error is not UnsupportedTemplateConstruct)
            {
                results.Add(new PostActionResult(
                    action,
                    PostActionOutcome.Failed,
                    error.Message,
                    JoinInstructions(action)));
            }
        }
        return results;
    }

    internal static string JoinInstructions(PostActionConfig action) => string.Join(
        '\n',
        action.ManualInstructions.Select(instruction => instruction.Text));

    async Task<PostActionResult> RunAction(
        PostActionConfig action,
        InstantiationResult result,
        IReadOnlyDictionary<string, string> symbols,
        CancellationToken cancellationToken) => action.ActionId.ToLowerInvariant() switch
        {
            "b17581d1-c5c9-4489-8f0a-004be667b814" => await AddReference.Run(action, result, store, cancellationToken),
            "d396686c-de0e-4de6-906d-291cd29fc5de" => await AddToSolution.Run(action, result),
            "cb9a6cf3-4f5c-4860-b9d2-03a574959774" => ChangePermissions.Run(action, result),
            "695a3659-eb40-4ff5-a6a6-c9c4e629fcb0" => await AddJsonProperty.Run(action, result, store, cancellationToken),
            "ac1156f7-bb77-4db8-b28f-24eebcca1e5c" => DisplayInstructions.Run(action, result),
            "3a7c4b45-1f5d-4a30-959a-51b88e82b5d2" => await RunScript.Run(action, result, scriptPolicy, cancellationToken),
            "84c0da21-51c8-4541-9940-6ca19af04ee6" => OpenInEditor.Run(action, result),
            "210d431b-a78b-4d2f-b762-4ed3e3ea9025" => await Restore.Run(action, result, cancellationToken: cancellationToken),
            _ => new PostActionResult(
                action,
                PostActionOutcome.Unknown,
                $"unknown actionId '{action.ActionId}' — reported as unhandled.",
                JoinInstructions(action))
        };
}
