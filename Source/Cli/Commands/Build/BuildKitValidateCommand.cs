// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Options for validating a kit.
/// </summary>
public sealed class BuildKitValidateSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the kit folder.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("The kit folder, holding build-kit.yaml or build-kit.json. Defaults to the current folder.")]
    public string? Path { get; set; }
}

/// <summary>
/// The result of validating a kit.
/// </summary>
/// <param name="Valid">Whether the kit is valid.</param>
/// <param name="Problems">Each problem, starting with where it is.</param>
/// <param name="Name">The kit's name, when it is valid.</param>
/// <param name="Version">The kit's version, when it is valid.</param>
public sealed record BuildKitValidationResult(bool Valid, IReadOnlyList<string> Problems, string? Name, string? Version);

/// <summary>
/// Validates a kit the way Studio and a builder host do, for kit authors.
/// </summary>
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand(
    "validate",
    "Validate a build kit for kit authors: the manifest against the format, that every file and folder it names is in the kit and not empty, that every skill has a SKILL.md, and that every placeholder is one a host fills. Changes nothing.",
    Branch = typeof(BuildBranch.Kit))]
[CliExample("build", "kit", "validate", "./my-kit")]
[CliExample("build", "kit", "validate", "--output", "json")]
public sealed class BuildKitValidateCommand : Command<BuildKitValidateSettings>
{
    /// <inheritdoc/>
    public override int Execute(CommandContext context, BuildKitValidateSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        BuildKitValidationResult result;
        try
        {
            var kit = BuildKit.Load(settings.Path ?? Directory.GetCurrentDirectory());
            result = new(true, [], kit.Name, kit.Version);
        }
        catch (InvalidBuildKit invalid)
        {
            result = new(false, invalid.Problems, null, null);
        }

        OutputFormatter.WriteObject(format, result, Render);
        return result.Valid ? ExitCodes.Success : ExitCodes.ValidationError;
    }

    static void Render(BuildKitValidationResult result)
    {
        if (result.Valid)
        {
            AnsiConsole.MarkupLineInterpolated($"[green]The kit {result.Name} {result.Version} is valid.[/]");
            return;
        }

        AnsiConsole.MarkupLine("[red]The kit is not valid:[/]");
        foreach (var problem in result.Problems) AnsiConsole.MarkupLineInterpolated($"  {problem}");
    }
}
