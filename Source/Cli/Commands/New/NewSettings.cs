// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.New;

/// <summary>
/// Settings for <c language="csharp">cratis new</c>. Deliberately does not extend <see cref="GlobalSettings"/>:
/// <c language="csharp">-o/--output</c> is the output directory with dotnet-new semantics, and machine-readable output
/// uses <c language="csharp">--format</c> instead of the CLI-wide output-format flag.
/// </summary>
public class NewSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the template to instantiate by short name; omit to list available templates.
    /// </summary>
    [CommandArgument(0, "[TEMPLATE]")]
    [Description("The template to instantiate, by short name (e.g. cratis). Omit to list available templates.")]
    public string? Template { get; set; }

    /// <summary>
    /// Gets or sets the name for the instantiated project. Defaults to the output directory name.
    /// </summary>
    [CommandOption("-n|--name <NAME>")]
    [Description("The name for the instantiated project (dotnet-new semantics). Defaults to the template default or the output directory name.")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the output directory. This diverges from the CLI-wide -o output-format flag on
    /// purpose: dotnet-new compatibility is the point of this command.
    /// </summary>
    [CommandOption("-o|--output <PATH>")]
    [Description("The output directory (dotnet-new semantics). Note: 'cratis new' deliberately uses -o for the output directory, not output format — use --format for machine-readable output.")]
    public string? Output { get; set; }

    /// <summary>
    /// Gets or sets the machine-readable output format.
    /// </summary>
    [CommandOption("--format <FORMAT>")]
    [Description("Machine-readable output format: table (default, rich terminal), plain (tab-separated) or json. Replaces the CLI-wide -o output-format flag on this command.")]
    [DefaultValue("table")]
    public string Format { get; set; } = "table";

    /// <summary>
    /// Gets or sets a value indicating whether to report what would be created without writing.
    /// </summary>
    [CommandOption("--dry-run")]
    [Description("Report what would be created and write nothing.")]
    [DefaultValue(false)]
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether writing into a non-empty output directory is allowed.
    /// </summary>
    [CommandOption("--force")]
    [Description("Allow writing into a non-empty output directory.")]
    [DefaultValue(false)]
    public bool Force { get; set; }

    /// <summary>
    /// Gets or sets the policy for script post actions.
    /// </summary>
    [CommandOption("--allow-scripts <POLICY>")]
    [Description("Policy for script post actions: yes runs them, no declines them, prompt asks when interactive (required explicitly when stdin is not a TTY).")]
    [DefaultValue("no")]
    public string AllowScripts { get; set; } = "no";

    /// <summary>
    /// Gets or sets the baseline whose symbol defaults apply.
    /// </summary>
    [CommandOption("--baseline <NAME>")]
    [Description("Select a template baseline (alternate symbol defaults).")]
    public string? Baseline { get; set; }

    /// <summary>
    /// Gets or sets the template package to use instead of the built-in catalogue.
    /// </summary>
    [CommandOption("--package <ID>")]
    [Description("Template package id to instantiate from, instead of the built-in catalogue.")]
    public string? Package { get; set; }

    /// <summary>
    /// Gets or sets the version of the template package.
    /// </summary>
    [CommandOption("--version <VERSION>")]
    [Description("Version of the template package: an exact version, or * for the latest (overrides the catalogue pin).")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets a local package folder or .nupkg to use, for unpublished templates.
    /// </summary>
    [CommandOption("--template-path <PATH>")]
    [Description("Local template package folder or .nupkg file, so unpublished packages can be exercised.")]
    public string? TemplatePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the template's parameter list should be shown instead of instantiating.
    /// </summary>
    [CommandOption("--parameters")]
    [Description("List the template's own parameters (descriptions, data types, choices, defaults) instead of instantiating.")]
    [DefaultValue(false)]
    public bool Parameters { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether prompting is disabled and defaults are always taken.
    /// </summary>
    [CommandOption("--no-prompts")]
    [Description("Never prompt for parameters; use bound values and defaults (non-interactive equivalent of every prompt).")]
    [DefaultValue(false)]
    public bool NoPrompts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether templates hidden behind group collapsing are also listed.
    /// </summary>
    [CommandOption("--all")]
    [Description("List all templates, including ones collapsed behind a higher-precedence group identity.")]
    [DefaultValue(false)]
    public bool All { get; set; }

    /// <summary>
    /// Validates the settings.
    /// </summary>
    /// <returns>Validation result.</returns>
    public override ValidationResult Validate()
    {
        if (AllowScripts != "yes" && AllowScripts != "no" && AllowScripts != "prompt")
        {
            return ValidationResult.Error("--allow-scripts must be one of: yes, no, prompt.");
        }
        if (Format != "table" && Format != "plain" && Format != "json")
        {
            return ValidationResult.Error("--format must be one of: table, plain, json.");
        }
        if (TemplatePath is not null && Package is not null)
        {
            return ValidationResult.Error("--template-path and --package cannot be combined.");
        }
        return ValidationResult.Success();
    }
}
