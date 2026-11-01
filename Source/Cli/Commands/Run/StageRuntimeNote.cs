// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// A non-blocking compiler diagnostic the Stage engine ignored.
/// </summary>
/// <param name="Code">The compiler diagnostic code.</param>
/// <param name="Message">The original diagnostic message.</param>
/// <param name="Source">The source file and location, when reported.</param>
/// <param name="Severity">The original string or numeric severity, including values from newer images.</param>
public sealed record StageRuntimeNote(string Code, string Message, string? Source = null, JsonElement? Severity = null);
