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
    [CommandOption("--warnings-as-errors|--warnaserror")]
    [Description("Treat compiler and completeness warnings as validation errors (exit 5). --warnaserror is an alias.")]
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
    [Description("Report source diagnostics for Module[[.Feature[[.Slice]]]] and its direct dependents; show whole-application counts separately.")]
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the completeness check selections to combine.
    /// </summary>
    [CommandOption("--check <SELECTION>")]
    [Description("Run structural completeness checks: data-bindings, input-surfaces, field-origins, query-keys, event-consumers, navigation, personas, purposes, privilege, enum-pinning, or all. Also accepts diagnostic codes. Repeat or comma-separate to combine selections. Findings are warnings, not proof of runtime completeness.")]
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

        // The command reports invalid check selections with ExitCodes.ValidationError, not Spectre's -1.
        ValidateChecks();

        return base.Validate();
    }

    internal ValidationResult ValidateChecks()
    {
        SelectedChecks = CompletenessChecks.None;
        foreach (var selection in Checks)
        {
            if (!CompletenessChecks.TryParse(selection, out var checks))
            {
                return ValidationResult.Error($"Unknown --check selection '{selection}'. Valid selections: data-bindings (PLAY0530, PLAY0531), input-surfaces (PLAY0532, PLAY0533), field-origins (PLAY0534), query-keys (PLAY0535), event-consumers (PLAY0536), navigation (PLAY0537), personas (PLAY0574, PLAY0575, PLAY0576), purposes (PLAY0602, PLAY0603, PLAY0604, PLAY0605, PLAY0606), privilege (PLAY0652), enum-pinning (PLAY0671), or all. Repeat --check or comma-separate names to combine selections.");
            }

            SelectedChecks = new(SelectedChecks.Selected.Union(checks.Selected));
        }

        return ValidationResult.Success();
    }
}
