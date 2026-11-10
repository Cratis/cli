// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// One command a service platform runs: the program, its arguments, and whether a failure stops the operation.
/// </summary>
/// <param name="Program">The program.</param>
/// <param name="Arguments">Its arguments.</param>
/// <param name="MayFail">Whether a non-zero exit is expected and ignored, such as unloading a service that is not loaded.</param>
public sealed record BuildServiceStep(string Program, IReadOnlyList<string> Arguments, bool MayFail = false)
{
    /// <inheritdoc/>
    public override string ToString() => string.Join(' ', [Program, .. Arguments]);
}
