// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Settings for commands that act on a project environment.
/// </summary>
public class EnvironmentSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the short form of <c language="csharp">--environment</c>.
    /// </summary>
    [CommandOption("-e <NAME>")]
    [Description("Environment to use (short for --environment). Overrides CRATIS_ENVIRONMENT and the selection made with 'cratis env select'.")]
    public string? EnvironmentShort { get; set; }

    /// <summary>
    /// Resolves the environment in effect: the option, <c language="csharp">CRATIS_ENVIRONMENT</c>, the selection file, then <c language="csharp">development</c>.
    /// </summary>
    /// <param name="environments">The project environments.</param>
    /// <returns>The environment and where the choice came from.</returns>
    public SelectedEnvironment ResolveEnvironment(ProjectEnvironments environments) =>
        environments.Resolve(SelectedEnvironmentName ?? EnvironmentShort);
}
