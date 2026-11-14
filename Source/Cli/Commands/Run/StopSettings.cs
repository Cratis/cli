// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Settings for the stop command.
/// </summary>
public class StopSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the name of the sandbox to stop.
    /// </summary>
    [CommandArgument(0, "[NAME]")]
    [Description("Name of the sandbox to stop, as 'cratis run --detach' reported it (cratis-stage-...). Optional when exactly one sandbox is running.")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether every running sandbox is stopped.
    /// </summary>
    [CommandOption("--all")]
    [Description("Stop every Stage sandbox that is running.")]
    [DefaultValue(false)]
    public bool All { get; set; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (Name is not null && All) return ValidationResult.Error("Give a sandbox name or --all, not both.");
        if (Name?.StartsWith(StageContainer.NamePrefix, StringComparison.Ordinal) == false)
        {
            return ValidationResult.Error($"Only Stage sandboxes can be stopped; their names start with '{StageContainer.NamePrefix}'.");
        }

        return base.Validate();
    }
}
