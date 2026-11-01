// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Reports an unexpected renderer failure without losing the original exception.
/// </summary>
/// <param name="phase">The renderer phase that failed.</param>
/// <param name="innerException">The original failure.</param>
internal sealed class RenderFailedUnexpectedly(string phase, Exception innerException)
    : Exception($"CLI-RENDER-027: Renderer failed unexpectedly during {phase} ({innerException.GetType().FullName}): {innerException.Message}. Nothing was planned or published. Report this to Cratis/Stage.", innerException)
{
    /// <summary>
    /// Gets the phase in which the renderer failed.
    /// </summary>
    public string Phase { get; } = phase;
}
