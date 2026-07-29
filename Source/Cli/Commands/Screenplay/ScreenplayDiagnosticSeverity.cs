// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents how severe a <see cref="ScreenplayDiagnostic"/> reported during generation is.
/// </summary>
public enum ScreenplayDiagnosticSeverity
{
    /// <summary>
    /// Informational — the generated document is complete; the diagnostic only adds context.
    /// </summary>
    Information = 0,

    /// <summary>
    /// A construct was recognized but could not be fully represented in the generated document.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Generation failed for the construct; the generated document does not describe the source faithfully.
    /// </summary>
    Error = 2
}
