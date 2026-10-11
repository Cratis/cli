// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>A corpus file selected for installation.</summary>
/// <param name="Path">The absolute path of the corpus file.</param>
/// <param name="Destination">The path relative to the managed root.</param>
/// <param name="Source">The corpus-relative source recorded in the managed marker.</param>
sealed record AiCorpusAsset(string Path, string Destination, string Source);
