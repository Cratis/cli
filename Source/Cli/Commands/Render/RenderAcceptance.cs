// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Resolves render acceptance independently of generation safety and publication authorization.
/// </summary>
/// <param name="acceptance">The CLI acceptance seam, returning a failure reason when acceptance fails.</param>
internal sealed class RenderAcceptance(Func<ScreenplayRenderPlan, bool, string?>? acceptance = null)
{
    /// <summary>
    /// The CLI acceptance seam, without guessing unpublished Stage members or inferring degradation from warnings.
    /// </summary>
    readonly Func<ScreenplayRenderPlan, bool, string?> _acceptance = acceptance ?? ((_, strict) => strict ? "Strict render acceptance is unavailable in the bundled Stage package." : null);

    /// <summary>
    /// Reports acceptance failure or preserves the accepted publication or check exit code.
    /// </summary>
    /// <param name="format">The resolved output format.</param>
    /// <param name="settings">The render settings containing the strict acceptance selection.</param>
    /// <param name="planned">The safely generated render plan.</param>
    /// <param name="acceptedExitCode">The exit code to return when acceptance succeeds.</param>
    /// <returns>The accepted exit code, or a validation error when acceptance fails.</returns>
    public int Complete(string format, RenderSettings settings, ScreenplayRenderPlan planned, int acceptedExitCode = ExitCodes.Success)
    {
        var failure = _acceptance(planned, settings.Strict);
        if (failure is null)
        {
            return acceptedExitCode;
        }

        OutputFormatter.WriteError(format, failure, "Inspect the render diagnostics and reports; safe publication is retained, and --check writes nothing", ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }
}
