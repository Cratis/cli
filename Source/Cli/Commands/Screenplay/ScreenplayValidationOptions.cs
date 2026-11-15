// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Validates command-line selections before resolving or compiling a Screenplay target.
/// </summary>
/// <param name="settings">The command-line selections.</param>
/// <param name="format">The output format used to report invalid selections.</param>
internal sealed class ScreenplayValidationOptions(ValidateScreenplaySettings settings, string format)
{
    /// <summary>
    /// Validates selections in diagnostic precedence order.
    /// </summary>
    /// <param name="context">The command context, including unrecognized arguments.</param>
    /// <returns>Whether all selections are valid.</returns>
    internal bool Validate(CommandContext context) =>
        ValidateRemainingArguments(context) && ValidateOpenQuestions() && ValidateChecks() && ValidateTarget();

    bool ValidateRemainingArguments(CommandContext context)
    {
        if (context.Remaining.Parsed.Count == 0 && context.Remaining.Raw.Count == 0)
        {
            return true;
        }

        var option = context.Remaining.Raw.Count > 0 ? context.Remaining.Raw[0] : context.Remaining.Parsed.First().Key;
        OutputFormatter.WriteError(format, $"Unknown option '{option}'.", "Run 'cratis screenplay validate --help' for usage", ExitCodes.ValidationErrorCode);
        return false;
    }

    bool ValidateOpenQuestions()
    {
        if (settings.OpenQuestions.Length > 1 || settings.OpenQuestions.Any(mode => mode is not ("report" or "fail")))
        {
            OutputFormatter.WriteError(format, "--open-questions requires report or fail and cannot be repeated.", "Run 'cratis screenplay validate --help' for usage", ExitCodes.ValidationErrorCode);
            return false;
        }

        return true;
    }

    bool ValidateChecks()
    {
        var checkValidation = settings.ValidateChecks();
        if (!checkValidation.Successful)
        {
            OutputFormatter.WriteError(format, checkValidation.Message!, "Run 'cratis screenplay validate --help' for usage", ExitCodes.ValidationErrorCode);
            return false;
        }

        return true;
    }

    bool ValidateTarget()
    {
        if (settings.Target is not null && !string.Equals(settings.Target, CratisRendering.TargetId, StringComparison.Ordinal))
        {
            OutputFormatter.WriteError(format, $"Renderer target '{settings.Target}' is not bundled with this CLI.", "Use --target cratis, or omit --target for target-neutral validation", ExitCodes.ValidationErrorCode);
            return false;
        }

        return true;
    }
}
