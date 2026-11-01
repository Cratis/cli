// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Settings for <c language="csharp">cratis env init</c>.
/// </summary>
public class EnvInitSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    [CommandArgument(0, "[NAME]")]
    [Description("Environment name; defaults to 'development'")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the secrets provider.
    /// </summary>
    [CommandOption("--secrets-provider <PROVIDER>")]
    [Description("Where secrets are kept: passphrase (default), azurekeyvault://<vault> or hashivault://<address>/<mount>[/<path>]")]
    public string? SecretsProvider { get; set; }
}
