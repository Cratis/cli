// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The options of <c language="shell">cratis build service</c>: the same as <c language="shell">cratis build run</c>, for the
/// build loop the service runs.
/// </summary>
public sealed class BuildServiceSettings : BuildAgentSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the service commits locally without pushing the branch.
    /// </summary>
    [CommandOption("--no-push")]
    [Description("Commit locally without pushing the branch.")]
    public bool NoPush { get; set; }
}
