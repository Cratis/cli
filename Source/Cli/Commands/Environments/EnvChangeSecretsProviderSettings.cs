// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Settings for <c language="csharp">cratis env change-secrets-provider</c>.
/// </summary>
public class EnvChangeSecretsProviderSettings : EnvironmentSettings
{
    /// <summary>
    /// Gets or sets the target provider.
    /// </summary>
    [CommandArgument(0, "<PROVIDER>")]
    [Description("Target secrets provider: passphrase, azurekeyvault://<vault> or hashivault://<address>/<mount>[/<path>]")]
    public string Provider { get; set; } = string.Empty;
}
