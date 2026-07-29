// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for the screenplay generate command.
/// </summary>
public class GenerateScreenplaySettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the solution, project, or folder to generate from.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Solution (.slnx, .sln), project (.csproj), or folder to read. Defaults to the current directory, searching upwards for a solution or project.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the file the generated Screenplay is written to.
    /// </summary>
    /// <remarks>
    /// Named <c>--file</c> rather than <c>-o</c> because <c>-o</c> is the global output format flag.
    /// </remarks>
    [CommandOption("--file <FILE>")]
    [Description("File to write the generated Screenplay to. Writes to standard output when not given.")]
    public string? File { get; set; }

    /// <summary>
    /// Gets or sets the domain the generated document belongs to.
    /// </summary>
    [CommandOption("--domain <NAME>")]
    [Description("Name of the domain the generated document belongs to. Defaults to the assembly or root namespace of the project, and to the solution name when several projects are read.")]
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the module every discovered feature is placed within.
    /// </summary>
    [CommandOption("--module <NAME>")]
    [Description("Name of the module every discovered feature is placed within. Defaults to the domain.")]
    public string? Module { get; set; }

    /// <summary>
    /// Gets or sets the number of leading namespace segments to skip when inferring features and slices.
    /// </summary>
    [CommandOption("--skip-segments <COUNT>")]
    [Description("Number of leading namespace segments to skip when inferring features and slices.")]
    public int? SkipSegments { get; set; }

    /// <summary>
    /// Gets the generation options these settings describe.
    /// </summary>
    /// <returns>The <see cref="ScreenplayGenerationOptions"/>.</returns>
    public ScreenplayGenerationOptions ToGenerationOptions() => new(Domain, Module, SkipSegments);
}
