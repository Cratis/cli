// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Init;

/// <summary>
/// What <c>cratis init</c> should write for one AI tool.
/// </summary>
/// <remarks>
/// A record rather than a parameter list because the three switches all read as bare booleans at the call
/// site, where transposing two of them produces a configuration that looks configured and is not.
/// </remarks>
/// <param name="Force">Whether to overwrite files that already exist.</param>
/// <param name="IncludeCommands">Whether to write the skill and slash-command/prompt files.</param>
/// <param name="IncludeContext">
/// Whether to add the <c>@CHRONICLE.md</c> reference to the tool's instruction file. False when that file is
/// generated from a shared corpus, where the edit would be overwritten on the next sync.
/// </param>
/// <param name="LlmContextJson">The serialized llm-context JSON to embed in skill files.</param>
public record AiToolConfiguration(
    bool Force,
    bool IncludeCommands,
    bool IncludeContext,
    string LlmContextJson);
