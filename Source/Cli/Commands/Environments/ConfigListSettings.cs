// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Settings for <c language="csharp">cratis config</c> and <c language="csharp">cratis config list</c>.
/// </summary>
public class ConfigListSettings : EnvironmentSettings
{
    /// <summary>
    /// Gets or sets the model path whose keys the environment must provide.
    /// </summary>
    [CommandOption("--missing <PATH>")]
    [Description("List the keys the model at PATH needs that the environment does not have (PATH: a key list file today; a Screenplay model once its binding convention ships)")]
    public string? Missing { get; set; }

    /// <summary>
    /// Gets or sets the model whose requirements are listed.
    /// </summary>
    [CommandOption("--requires <PATH>")]
    [Description("List everything the Screenplay model at PATH needs from the environment - identity provider keys, named secrets and the settings of its extensions - with whether each is set. Values are never printed")]
    public string? Requires { get; set; }
}
