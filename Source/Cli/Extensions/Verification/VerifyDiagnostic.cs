// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// One finding of a check.
/// </summary>
/// <param name="Check">The check that found it.</param>
/// <param name="Code">The diagnostic code: an <c language="text">EXTV</c> code of the CLI or a <c language="text">PLAY</c> code of the compiler.</param>
/// <param name="Severity">How serious it is.</param>
/// <param name="Message">What is wrong and what to do about it.</param>
/// <param name="Path">The package-relative path it concerns, if any.</param>
/// <param name="Line">The line in <c language="text">contract.play</c> it concerns, if any.</param>
public record VerifyDiagnostic(string Check, string Code, VerifySeverity Severity, string Message, string? Path = null, int? Line = null);
