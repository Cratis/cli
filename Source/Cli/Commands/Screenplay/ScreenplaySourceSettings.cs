// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Source-adapter options shared by generation and conformance checks.
/// </summary>
public class ScreenplaySourceSettings : GlobalSettings
{
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
    /// Gets or sets the project-relative folder beneath which feature and slice placement is derived.
    /// </summary>
    [CommandOption("--feature-root <PATH>")]
    [Description("Project-relative folder beneath which feature and slice placement is derived. Supported by Marten and Critter Stack.")]
    public string? FeatureRoot { get; set; }

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
    /// Gets or sets a value indicating whether each feature is placed in a module named after the outermost segment of its namespace.
    /// </summary>
    [CommandOption("--modules-from-namespace-roots")]
    [Description("Name the module of each feature after the outermost segment of its namespace, instead of placing every feature in one module. Combine with --skip-segments when every slice shares a root namespace.")]
    public bool ModulesFromNamespaceRoots { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Arc generation includes constructs not yet executable.
    /// </summary>
    [CommandOption("--authoring-only-constructs")]
    [Description("With Arc, emit constructs not yet executable (operations, routes, reads). The document then has no executable model (PLAY0268); intended for extraction and review. Defaults to false.")]
    public bool AuthoringOnlyConstructs { get; set; }

    /// <summary>
    /// Gets the generation options these settings describe.
    /// </summary>
    /// <returns>The source generation options.</returns>
    public ScreenplayGenerationOptions ToGenerationOptions() =>
        new(Domain, Module, SkipSegments, ModulesFromNamespaceRoots, Provider)
        {
            FeatureRoot = FeatureRoot,
            TargetFramework = Framework,
            AuthoringOnlyConstructs = AuthoringOnlyConstructs
        };
}
