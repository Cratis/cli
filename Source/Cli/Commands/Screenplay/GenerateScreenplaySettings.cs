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
    [Description("Solution (.slnx, .sln, .slnf), project (.csproj), or folder to read. Defaults to the current directory, searching upwards for a solution or project.")]
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
    /// Gets or sets the source framework provider used for generation.
    /// </summary>
    [CommandOption("--provider <PROVIDER>")]
    [Description("Source framework provider: auto, arc, marten, or critter-stack. Defaults to auto detection.")]
    public string Provider { get; set; } = ScreenplayProviders.Auto;

    /// <summary>
    /// Gets or sets the target framework to load from multi-targeted projects.
    /// </summary>
    [CommandOption("--framework <TFM>")]
    [Description("Target framework to load from multi-targeted projects. Required when any application project targets several frameworks.")]
    public string? Framework { get; set; }

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
    /// Gets or sets a value indicating whether each feature is placed in a module named after the outermost segment
    /// of its namespace.
    /// </summary>
    /// <remarks>
    /// An application whose namespaces already name its modules otherwise comes back as one module holding every
    /// feature, because a module is only taken from the namespaces when nothing else could name one. This asks for
    /// it. Naming a module with <c>--module</c> still collapses the document into that one.
    /// <para>
    /// The outermost segment is regularly the root namespace every slice shares, which names one module again —
    /// <c>--skip-segments 1</c> then moves the modules down to the segment that tells them apart.
    /// </para>
    /// </remarks>
    [CommandOption("--modules-from-namespace-roots")]
    [Description("Name the module of each feature after the outermost segment of its namespace, instead of placing every feature in one module. Combine with --skip-segments when every slice shares a root namespace.")]
    public bool ModulesFromNamespaceRoots { get; set; }

    /// <summary>
    /// Gets the generation options these settings describe.
    /// </summary>
    /// <returns>The <see cref="ScreenplayGenerationOptions"/>.</returns>
    public ScreenplayGenerationOptions ToGenerationOptions() =>
        new(Domain, Module, SkipSegments, ModulesFromNamespaceRoots, Provider)
        {
            TargetFramework = Framework
        };
}
