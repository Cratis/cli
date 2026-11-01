// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Settings for <c language="csharp">cratis config get</c>.
/// </summary>
public class ConfigGetSettings : ConfigKeySettings
{
    /// <summary>
    /// Gets or sets a value indicating whether a secret is revealed.
    /// </summary>
    [CommandOption("--show-secret")]
    [Description("Print the value of a secret instead of [secret]")]
    [DefaultValue(false)]
    public bool ShowSecret { get; set; }
}
