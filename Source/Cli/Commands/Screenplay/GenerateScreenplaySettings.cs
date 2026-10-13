// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for the screenplay generate command.
/// </summary>
public class GenerateScreenplaySettings : ScreenplaySourceSettings
{
    /// <summary>
    /// Gets or sets the solution, project, or folder to generate from.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Solution (.slnx, .sln, .slnf), project (.csproj), or folder to read. Defaults to the current directory, searching upwards for a solution or project.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the file the generated Screenplay is written to.
    /// </summary>
    /// <remarks>
    /// Named <c language="csharp">--file</c> rather than <c language="csharp">-o</c> because <c language="csharp">-o</c> is the global output format flag.
    /// </remarks>
    [CommandOption("--file <FILE>")]
    [Description("File to write the generated Screenplay to. Defaults to Screenplay.play in the current directory — or Screenplay-1.play, Screenplay-2.play, and so on when that already exists.")]
    public string? File { get; set; }
}
