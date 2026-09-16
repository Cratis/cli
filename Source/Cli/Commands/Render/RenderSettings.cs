// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Settings for the render command.
/// </summary>
public class RenderSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the document or folder to render.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Screenplay (.play) file, or folder representing one logical application. Defaults to the current directory without --workspace; mutually exclusive with --workspace.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the canonical workspace envelope file, instead of a document or folder.
    /// </summary>
    [CommandOption("--workspace <FILE>")]
    [Description("Canonical Screenplay workspace file (maximum 32 MiB). Mutually exclusive with PATH; preserves its application name and identities.")]
    public string? Workspace { get; set; }

    /// <summary>
    /// Gets or sets the statically bundled renderer target.
    /// </summary>
    [CommandOption("--target <TARGET>")]
    [Description("Renderer target. The initial bundled target is 'cratis'.")]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the directory to publish into.
    /// </summary>
    [CommandOption("--destination <DIRECTORY>")]
    [Description("Directory to publish managed artifacts into. Defaults to './out'.")]
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets the destination-independent application identity.
    /// </summary>
    [CommandOption("--name <NAME>")]
    [Description("Required application identity for plain source; optional with --workspace, where it must equal the workspace name. Defaults the project name and root namespace.")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the generated project and solution name without changing application identity.
    /// </summary>
    [CommandOption("--project-name <NAME>")]
    [Description("Generated project and solution name. Defaults to the application name.")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Gets or sets the root namespace requested from the rendering profile.
    /// </summary>
    [CommandOption("--root-namespace <NAMESPACE>")]
    [Description("Root namespace for generated C#. Defaults to the application name independently of --project-name; does not change application identity.")]
    public string? RootNamespace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether modified active managed artifacts may be replaced.
    /// </summary>
    [CommandOption("--force")]
    [Description("Replace modified active managed artifacts; never overwrite unmanaged files or remove modified stale files.")]
    public bool Force { get; set; }
}
