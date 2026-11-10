// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Options for running the build loop, or a modeling session on a board.
/// </summary>
public sealed class BuildRunSettings : BuildAgentSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to stop after one slice.
    /// </summary>
    [CommandOption("--once")]
    [Description("Stop after building one slice.")]
    public bool Once { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether commits stay local.
    /// </summary>
    [CommandOption("--no-push")]
    [Description("Commit locally without pushing to origin. Studio cannot open a pull request for a commit it cannot see.")]
    public bool NoPush { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to model on a board instead of building.
    /// </summary>
    [CommandOption("--modeling")]
    [Description("Connect the agent to a Studio board for modeling instead of building: people hand it work by mentioning it in the board chat.")]
    public bool Modeling { get; set; }

    /// <summary>
    /// Gets or sets the board.
    /// </summary>
    [CommandOption("--board <BOARD>")]
    [Description("The event model board to model on, with --modeling: its identifier, or its address in Studio.")]
    public string? Board { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the agent acts without asking.
    /// </summary>
    [CommandOption("--unattended")]
    [Description("With --modeling, let the agent change the board without a person confirming its proposal first.")]
    public bool Unattended { get; set; }

    /// <summary>
    /// Gets or sets what the agent is called on the board.
    /// </summary>
    [CommandOption("--name <NAME>")]
    [Description("With --modeling, what the agent is called and mentioned by on the board. Defaults to the agent's name.")]
    public string? Name { get; set; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (Modeling && Exec is not null) return ValidationResult.Error("A modeling session needs a coding agent; --exec cannot model.");
        if (Modeling && ModelingSessionOptions.BoardFrom(Board) is null) return ValidationResult.Error("Name the board with --board <event model id or board address>.");
        if (Modeling && Name is not null && !ModelingSessionOptions.IsValidName(Name))
        {
            return ValidationResult.Error("--name takes letters, digits, spaces, '.', '-' and '_', starts with a letter or digit, and has at most 60 characters.");
        }

        if (!Modeling && (Board is not null || Unattended)) return ValidationResult.Error("--board and --unattended go with --modeling.");
        return base.Validate();
    }
}
