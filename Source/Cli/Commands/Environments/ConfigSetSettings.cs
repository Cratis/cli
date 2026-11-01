// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Settings for <c language="csharp">cratis config set</c>.
/// </summary>
public class ConfigSetSettings : EnvironmentSettings
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    [CommandArgument(0, "<KEY>")]
    [Description("Configuration key, for example auth:clientId")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value. Not allowed for secrets.
    /// </summary>
    [CommandArgument(1, "[VALUE]")]
    [Description("The value. Omit with --secret: a secret is read from stdin or a hidden prompt so it stays out of shell history.")]
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the value is a secret.
    /// </summary>
    [CommandOption("--secret")]
    [Description("Treat the value as a secret: read it from stdin or a hidden prompt (never from an argument) and protect it with the environment's secrets provider")]
    [DefaultValue(false)]
    public bool Secret { get; set; }
}
