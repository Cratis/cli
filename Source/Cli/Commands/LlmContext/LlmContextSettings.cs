// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.LlmContext;

/// <summary>
/// Settings for the <see cref="LlmContextCommand"/>.
/// </summary>
public class LlmContextSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to output the JSON Schema for the
    /// LLM context descriptor instead of the live descriptor. Useful for AI tools
    /// that want to understand the structure of the output before parsing it.
    /// </summary>
    [CommandOption("--schema")]
    [Description("Output the JSON Schema for the llm-context output format instead of the live descriptor")]
    [DefaultValue(false)]
    public bool Schema { get; set; }
}
