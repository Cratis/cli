// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Settings for the prologue interpret command.
/// </summary>
public class InterpretPrologueSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the folder holding the capture files to interpret.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Folder holding the capture (.jsonl) files. Defaults to the configured JSON output directory when a cratis-prologue.json is found, otherwise the current directory.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the file the generated Screenplay is written to.
    /// </summary>
    [CommandOption("--file <FILE>")]
    [Description("File to write the generated Screenplay to. Defaults to <SystemName>.play in the current directory.")]
    public string? File { get; set; }

    /// <summary>
    /// Gets or sets the Prologue the captures belong to.
    /// </summary>
    [CommandOption("--prologue-id <ID>")]
    [Description("The Prologue the captures belong to. Defaults from cratis-prologue.json when one is present in PATH or the current directory.")]
    public Guid? PrologueId { get; set; }
}
