// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The outcome of a finished child process.
/// </summary>
/// <param name="ExitCode">The exit code.</param>
/// <param name="Output">Everything written to standard output.</param>
/// <param name="Error">Everything written to standard error.</param>
public record ProcessResult(int ExitCode, string Output, string Error);
