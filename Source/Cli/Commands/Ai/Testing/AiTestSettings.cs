// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai.Testing;

/// <summary>
/// Options for isolated deterministic harness conformance checks.
/// </summary>
public sealed class AiTestSettings : GlobalSettings
{
    /// <summary>Gets or sets the local corpus checkout.</summary>
    [CommandOption("--source <PATH>")]
    [Description("Local Cratis/AI checkout. Env: CRATIS_AI_SOURCE")]
    public string? Source { get; set; }

    /// <summary>Gets or sets the comma-separated harness selection.</summary>
    [CommandOption("--harnesses <NAMES>")]
    [Description("Harnesses to test; defaults to supported adapters in the installed configuration")]
    public string? Harnesses { get; set; }

    /// <summary>Gets or sets the selected fixture names.</summary>
    [CommandOption("--fixture <NAME>")]
    [Description("Fixture name to select; repeat to select multiple fixtures")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Spectre.Console.Cli uses arrays for repeatable options.")]
    public string[] Fixture { get; set; } = [];

    /// <summary>Gets or sets an alternate fixtures root.</summary>
    [CommandOption("--fixtures <PATH>")]
    [Description("Fixtures root; defaults to Evaluations/conformance in the corpus")]
    public string? Fixtures { get; set; }

    /// <summary>Gets or sets the timeout ceiling in seconds.</summary>
    [CommandOption("--timeout <SECONDS>")]
    [Description("Lower the fixture timeout bound")]
    public double? Timeout { get; set; }

    /// <summary>Gets or sets the model budget ceiling.</summary>
    [CommandOption("--max-budget-usd <USD>")]
    [Description("Lower the fixture model budget bound")]
    public decimal? MaxBudgetUsd { get; set; }

    /// <summary>Gets or sets whether redacted transcripts are retained in temporary output.</summary>
    [CommandOption("--keep-transcripts")]
    [Description("Keep redacted transcripts only under each run's temporary output directory")]
    public bool KeepTranscripts { get; set; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (Timeout is { } timeout)
        {
            if (!double.IsFinite(timeout)) return ValidationResult.Error("--timeout must be a positive finite number.");
            if (timeout <= 0) return ValidationResult.Error("--timeout must be a positive finite number.");
        }
        if (MaxBudgetUsd is <= 0) return ValidationResult.Error("--max-budget-usd must be positive.");
        if (Output is not ("auto" or "text" or "json")) return ValidationResult.Error("--output must be text or json.");
        return base.Validate();
    }
}
