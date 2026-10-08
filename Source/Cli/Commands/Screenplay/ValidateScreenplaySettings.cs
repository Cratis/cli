// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for the screenplay validate command.
/// </summary>
public class ValidateScreenplaySettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the document or folder to compile.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Root Screenplay (.play) file with its imports, or folder to compile every .play file beneath as one application. Defaults to the current directory.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether compiler warnings should fail validation.
    /// </summary>
    [CommandOption("--warnings-as-errors")]
    [Description("Treat compiler warnings as validation errors.")]
    public bool WarningsAsErrors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model must also bind into an executable semantic model.
    /// </summary>
    [CommandOption("--executable")]
    [Description("Also bind the model into an executable semantic model and fail when it does not bind. Does not render or write files.")]
    public bool Executable { get; set; }

    /// <summary>
    /// Gets or sets the case-sensitive dotted module, feature or slice to report with its direct dependents.
    /// </summary>
    [CommandOption("--scope <ADDRESS>")]
    [Description("Report source diagnostics for Module[.Feature[.Slice]] and its direct dependents; show whole-application counts separately.")]
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the completeness check selections to combine.
    /// </summary>
    [CommandOption("--check <SELECTION>")]
    [Description("Run structural completeness checks by comma-separated names, PLAY0530–PLAY0537, or all. Repeat to combine selections. Findings are warnings, not proof of runtime completeness.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Spectre.Console.Cli uses arrays for repeatable options.")]
    public string[] Checks { get; set; } = [];

    /// <summary>
    /// Gets the union of the selected completeness checks after settings validation.
    /// </summary>
    public CompletenessChecks SelectedChecks { get; private set; } = CompletenessChecks.None;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        SelectedChecks = CompletenessChecks.None;
        if (Scope is not null && Executable)
        {
            return ValidationResult.Error("--scope selects source diagnostics and cannot be combined with --executable. Check executable binding for the whole application separately.");
        }
        foreach (var selection in Checks)
        {
            if (!CompletenessChecks.TryParse(selection, out var checks))
            {
                return ValidationResult.Error("--check requires comma-separated data-bindings (PLAY0530, PLAY0531), input-surfaces (PLAY0532, PLAY0533), field-origins (PLAY0534), query-keys (PLAY0535), event-consumers (PLAY0536), navigation (PLAY0537), or all.");
            }

            SelectedChecks = new(SelectedChecks.Selected.Union(checks.Selected));
        }

        return base.Validate();
    }
}
