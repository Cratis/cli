// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Defines Screenplay document-set compilation and pure target artifact planning.
/// </summary>
internal interface IScreenplayPlanning
{
    /// <summary>
    /// Compiles and plans one logical Screenplay application without mutating its destination.
    /// </summary>
    /// <param name="request">The trusted source, application identity, and target request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The complete plan and diagnostics.</returns>
    Task<ScreenplayRenderPlan> Plan(ScreenplayRenderRequest request, CancellationToken cancellationToken);
}
