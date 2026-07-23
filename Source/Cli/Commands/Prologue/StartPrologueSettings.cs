// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Settings for the prologue start command.
/// </summary>
public class StartPrologueSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets where the configuration file is written — a file path or an existing directory.
    /// </summary>
    [CommandOption("--file <PATH>")]
    [Description("Where to write the configuration — a file path or a directory. Defaults to cratis-prologue.json in the current directory.")]
    public string? File { get; set; }
}
