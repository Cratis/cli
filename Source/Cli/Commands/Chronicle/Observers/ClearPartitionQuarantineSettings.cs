// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Settings for clearing the quarantine of a single failed partition.
/// </summary>
public class ClearPartitionQuarantineSettings : PartitionCommandSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to only clear the quarantine, without starting a retry.
    /// </summary>
    [CommandOption("--no-retry")]
    [Description("Only clear the quarantine and reset the retry budget; do not start a retry")]
    public bool NoRetry { get; set; }
}
