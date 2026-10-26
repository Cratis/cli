// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

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
    /// Gets or sets the release-baseline file for workspace input.
    /// </summary>
    [CommandOption("--baseline <FILE>")]
    [Description("Release baseline for --workspace only. Defaults to .screenplay/release-baseline.json beside the workspace file. Baseline warnings do not change the exit code.")]
    public string? Baseline { get; set; }

    /// <summary>
    /// Gets or sets the statically bundled renderer target.
    /// </summary>
    [CommandOption("--target <TARGET>")]
    [Description("Renderer target. The initial bundled target is 'cratis'.")]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the repeatable module, feature, or slice addresses to render.
    /// </summary>
    [CommandOption("--scope <ADDRESS>")]
    [Description("Render only this authored module, feature, or slice address. Repeat to render several scopes. Release-baseline warnings still cover the whole application.")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Spectre.Console.Cli uses arrays for repeatable options.")]
    public string[] Scope { get; set; } = [];

    /// <summary>
    /// Gets or sets the repeatable read models to migrate from a classic reducer to a self-reducing read model.
    /// </summary>
    [CommandOption("--migrate-reducer <READMODEL>")]
    [Description("Migrate this read model (Module.Feature.Slice.ReadModel or its semantic id) from its classic IReducerFor<T> reducer to a self-reducing read model that keeps the observer identity. Deletes the old reducer file with the render. Repeat for several read models; needs a whole-application render and cannot be combined with --scope.")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Spectre.Console.Cli uses arrays for repeatable options.")]
    public string[] MigrateReducer { get; set; } = [];

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
    /// Gets or sets the approved package set document for custom runtime packages.
    /// </summary>
    [CommandOption("--package-set <FILE>")]
    [Description("Approved package set (JSON) naming the custom runtime packages the application's ui profile may use, beyond core, scene.web and Cratis.Components. The rendered frontend installs and loads exactly these packages.")]
    public string? PackageSet { get; set; }

    /// <summary>
    /// Gets or sets the repeatable lines every generated code file starts with.
    /// </summary>
    [CommandOption("--file-header <LINE>")]
    [Description("Line every generated C#, TypeScript and JavaScript file starts with, as plain text without comment markers; the renderer writes it as a // comment. Repeat for several lines, or separate lines with \\n. {year} becomes the current UTC year. Defaults to no header. Mutually exclusive with --file-header-file.")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Spectre.Console.Cli uses arrays for repeatable options.")]
    public string[] FileHeader { get; set; } = [];

    /// <summary>
    /// Gets or sets the text file holding the lines every generated code file starts with.
    /// </summary>
    [CommandOption("--file-header-file <FILE>")]
    [Description("Text file whose lines every generated C#, TypeScript and JavaScript file starts with, as plain text without comment markers. {year} becomes the current UTC year. Mutually exclusive with --file-header.")]
    public string? FileHeaderFile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether publication is checked without writing or recovering.
    /// </summary>
    [CommandOption("--check")]
    [Description("Check without writing: exit 0 when current, 6 when changes are pending, or 5 for planning, publication, or recovery refusals.")]
    public bool Check { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether strict render acceptance is required after safe publication.
    /// </summary>
    [CommandOption("--strict")]
    [Description("Require strict acceptance after safe publication. Exit precedence: errors 1/5/10 > strict_unaccepted 9 > changes_pending 6 > success 0; report all applicable conditions. --check never writes. Requires bundled Stage acceptance support.")]
    public bool Strict { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether modified active managed artifacts may be replaced.
    /// </summary>
    [CommandOption("--force")]
    [Description("Replace modified active managed artifacts; never overwrite unmanaged files or remove modified stale files.")]
    public bool Force { get; set; }
}
