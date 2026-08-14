// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents what rendering a set of Screenplay documents produced.
/// </summary>
/// <param name="Documents">How many documents were rendered.</param>
/// <param name="Diagnostics">Everything the compiler reported about the documents.</param>
/// <param name="Reported">
/// Everything the renderer could not carry into the rendered application, in the order it was reported.
/// </param>
/// <remarks>
/// <see cref="Reported"/> is the point of the command as much as the files are. A Screenplay document states more
/// than any one target can express, and the promise is that whatever does not survive the crossing is said out
/// loud rather than quietly left behind — so it is carried back as a result, not written past the user.
/// </remarks>
public record RenderedScreenplay(int Documents, IReadOnlyList<ScreenplayDiagnostic> Diagnostics, IReadOnlyList<string> Reported);
