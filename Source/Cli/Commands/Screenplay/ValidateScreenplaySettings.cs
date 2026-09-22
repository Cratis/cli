// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for the screenplay validate command.
/// </summary>
public class ValidateScreenplaySettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the document or folder to compile.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Screenplay (.play) file, or folder to compile every .play file beneath as one application. Defaults to the current directory.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether compiler warnings should fail validation.
    /// </summary>
    [CommandOption("--warnings-as-errors")]
    [Description("Treat compiler warnings as validation errors.")]
    public bool WarningsAsErrors { get; set; }
}
