// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for admitting a Screenplay release baseline.
/// </summary>
public class AdmitBaselineScreenplaySettings : BaselineScreenplaySettings
{
    /// <summary>
    /// Gets or sets the release label.
    /// </summary>
    [CommandOption("--release <LABEL>")]
    [Description("Required release label for the admitted event revisions.")]
    public string? Release { get; set; }

    /// <summary>
    /// Gets or sets the canonical shipped executable semantic model file.
    /// </summary>
    [CommandOption("--from-esm <FILE>")]
    [Description("Canonical shipped executable semantic model to admit instead of the current model.")]
    public string? FromEsm { get; set; }

    /// <inheritdoc/>
    public override ValidationResult Validate() => string.IsNullOrWhiteSpace(Release)
        ? ValidationResult.Error("--release is required for baseline admission.")
        : base.Validate();
}
