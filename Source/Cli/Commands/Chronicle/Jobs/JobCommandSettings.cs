// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Jobs;

/// <summary>
/// Settings for commands that target a specific job.
/// </summary>
public class JobCommandSettings : JobsSettings
{
    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    [CommandArgument(0, "<JOB_ID>")]
    [Description("Job identifier (from 'cratis chronicle jobs list')")]
    public string JobId { get; set; } = string.Empty;
}
