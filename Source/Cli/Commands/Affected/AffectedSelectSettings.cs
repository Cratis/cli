// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected;

namespace Cratis.Cli.Commands.Affected;

/// <summary>
/// Options shared by the commands that select tests for a change.
/// </summary>
public class AffectedSelectSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the ref the change is measured against.
    /// </summary>
    [CommandOption("--from <REF>")]
    [Description("The ref the change is measured against, normally the default branch. The merge base with --to is used. Default: origin/main")]
    [DefaultValue("origin/main")]
    public string From { get; set; } = "origin/main";

    /// <summary>
    /// Gets or sets the ref being tested.
    /// </summary>
    [CommandOption("--to <REF>")]
    [Description("The ref being tested. Default: HEAD")]
    [DefaultValue("HEAD")]
    public string To { get; set; } = "HEAD";

    /// <summary>
    /// Gets or sets the mode.
    /// </summary>
    [CommandOption("--mode <MODE>")]
    [Description("pr selects what the change can reach; merge and nightly run everything, and merge also refreshes the coverage maps. Default: pr")]
    [DefaultValue("pr")]
    public string Mode { get; set; } = "pr";

    /// <summary>
    /// Gets or sets the selection level.
    /// </summary>
    [CommandOption("--level <LEVEL>")]
    [Description("project runs affected test projects in full and needs no coverage maps; test also selects individual tests from the maps. Default: test")]
    [DefaultValue("test")]
    public string Level { get; set; } = "test";

    /// <summary>
    /// Gets or sets a value indicating whether a full run is forced.
    /// </summary>
    [CommandOption("--run-all")]
    [Description("Force a full run, for example because the pull request carries the run-all-tests label.")]
    [DefaultValue(false)]
    public bool RunAll { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether building and discovering tests is skipped.
    /// </summary>
    [CommandOption("--no-discovery")]
    [Description("Do not build affected test projects to discover new tests and verify the generated filters. Faster, but new tests are only caught through the added-file policy.")]
    [DefaultValue(false)]
    public bool NoDiscovery { get; set; }

    /// <summary>
    /// Gets or sets the target framework.
    /// </summary>
    [CommandOption("--framework <TFM>")]
    [Description("The target framework to build and run, for test projects that have several.")]
    public string? Framework { get; set; }

    /// <summary>
    /// Gets or sets the build configuration.
    /// </summary>
    [CommandOption("--configuration <CONFIGURATION>")]
    [Description("The build configuration. Default: Release")]
    [DefaultValue("Release")]
    public string Configuration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets the folder the selection report is written to.
    /// </summary>
    [CommandOption("--report-directory <PATH>")]
    [Description("Where selection.json and selection.md are written. Default: affected-report")]
    [DefaultValue("affected-report")]
    public string ReportDirectory { get; set; } = "affected-report";

    /// <summary>
    /// Converts the options to planner options.
    /// </summary>
    /// <returns>The planner options.</returns>
    /// <exception cref="ArgumentException">The mode or level is not one of the known values.</exception>
    public PlanOptions ToPlanOptions() => new(
        From,
        To,
        Mode.ToLowerInvariant() switch
        {
            "pr" => PlanMode.PullRequest,
            "merge" => PlanMode.Merge,
            "nightly" => PlanMode.Nightly,
            _ => throw new ArgumentException($"Unknown mode '{Mode}'; use pr, merge or nightly.", nameof(Mode))
        },
        Level.ToLowerInvariant() switch
        {
            "project" => SelectionLevel.Project,
            "test" => SelectionLevel.Test,
            _ => throw new ArgumentException($"Unknown level '{Level}'; use project or test.", nameof(Level))
        },
        RunAll,
        !NoDiscovery);
}
