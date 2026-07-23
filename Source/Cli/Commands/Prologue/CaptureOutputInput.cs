// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Represents where the extractor writes captured data, as entered in the Prologue setup wizard.
/// </summary>
/// <param name="Kind">The kind of output — the Prologue Receiver API or rolling JSON capture files.</param>
/// <param name="ApiEndpoint">The base address of the Prologue Receiver, used when <paramref name="Kind"/> is <see cref="OutputKind.Api"/>.</param>
/// <param name="JsonDirectory">The directory capture files are written to, used when <paramref name="Kind"/> is <see cref="OutputKind.Json"/>.</param>
public record CaptureOutputInput(OutputKind Kind, string ApiEndpoint, string JsonDirectory);
