// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Affected;

/// <summary>
/// Options for replaying merged changes.
/// </summary>
public sealed class AffectedReplaySettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the branch to read history from.
    /// </summary>
    [CommandOption("--branch <REF>")]
    [Description("The branch whose first-parent history is replayed. Default: HEAD")]
    [DefaultValue("HEAD")]
    public string Branch { get; set; } = "HEAD";

    /// <summary>
    /// Gets or sets the number of changes to replay.
    /// </summary>
    [CommandOption("--count <COUNT>")]
    [Description("How many of the latest merged changes to replay. Default: 100")]
    [DefaultValue(100)]
    public int Count { get; set; } = 100;

    /// <summary>
    /// Gets or sets the file listing the tests that failed in the real full runs.
    /// </summary>
    [CommandOption("--failures <FILE>")]
    [Description("A JSON file mapping a commit to the test classes or tests that failed in its real full run, for example {\"<sha>\": [\"Ns.Class\"]}. Without it the study measures selection size only and cannot prove zero misses.")]
    public string? Failures { get; set; }

    /// <summary>
    /// Gets or sets the folder the report is written to.
    /// </summary>
    [CommandOption("--report-directory <PATH>")]
    [Description("Where replay.md and replay.json are written. Default: affected-report")]
    [DefaultValue("affected-report")]
    public string ReportDirectory { get; set; } = "affected-report";
}
