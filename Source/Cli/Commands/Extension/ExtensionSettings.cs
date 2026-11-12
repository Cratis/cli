// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Environments;

namespace Cratis.Cli.Commands.Extension;

/// <summary>
/// Settings every <c>cratis extension</c> command shares: the model root, an alternative package source and the environment that holds registry credentials.
/// </summary>
public class ExtensionSettings : EnvironmentSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionSettings"/> class.
    /// </summary>
    public ExtensionSettings() => Output = OutputFormats.Plain;

    /// <summary>
    /// Gets or sets the model root.
    /// </summary>
    [CommandOption("--root <PATH>")]
    [Description("Model root holding the .play files and screenplay.lock. Defaults to the current directory, honoring the configured Screenplay MCP root.")]
    public string? Root { get; set; }

    /// <summary>
    /// Gets or sets the package source to use instead of the registries the references name.
    /// </summary>
    [CommandOption("--source <FOLDER|oci://HOST/PREFIX>")]
    [Description("Fetch packages from a vendored folder or a private registry that mirrors them, instead of from the registries the references name.")]
    public string? Source { get; set; }
}
