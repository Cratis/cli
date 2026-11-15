// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Options for installing a build kit into a project.
/// </summary>
public sealed class BuildInitSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the kit.
    /// </summary>
    [CommandOption("--kit <KIT>")]
    [Description("The kit: the name of a kit Studio registers (such as cratis-csharp or node), a Git URL (add #<branch, tag or commit> to pin it), or a folder.")]
    public string Kit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the agent.
    /// </summary>
    [CommandOption("--agent <AGENT>")]
    [Description("The coding agent to install the kit's instructions and skills for, and to build with by default.")]
    public string? Agent { get; set; }

    /// <summary>
    /// Gets or sets the folder inside the kit's repository that holds the manifest.
    /// </summary>
    [CommandOption("--kit-folder <PATH>")]
    [Description("The folder inside the kit's repository that holds build-kit.yaml. Defaults to the repository's root.")]
    public string? KitFolder { get; set; }

    /// <summary>
    /// Gets or sets the project folder.
    /// </summary>
    [CommandOption("--project <PATH>")]
    [Description("The project folder. Defaults to the current folder.")]
    public string? Project { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the project is an existing codebase.
    /// </summary>
    [CommandOption("--existing")]
    [Description("Treat the project as an existing codebase even when it is empty. A project that is not empty always is: the kit is not scaffolded, the first build learns and records the project's conventions, and later builds reuse them.")]
    public bool Existing { get; set; }

    /// <summary>
    /// Gets or sets the Studio registered kits are read from.
    /// </summary>
    [CommandOption("--studio <URL>")]
    [Description("The Studio to read registered kits from. Defaults to the Studio this machine is connected to.")]
    public string? Studio { get; set; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Kit)) return ValidationResult.Error("Name the kit with --kit <name|git url|folder>.");
        if (Agent is not null && !BuildAgents.IsKnown(Agent)) return ValidationResult.Error($"'{Agent}' is not an agent this CLI runs. Use one of {string.Join(", ", BuildAgents.Names)}.");
        return base.Validate();
    }
}

/// <summary>
/// Installs a build kit into a project and prepares it for the chosen agent.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand(
    "init",
    "Install a build kit into a project: validate its manifest, scaffold a new project (or prepare an existing codebase with --existing), and install the kit's instructions and skills where the chosen agent reads them.",
    Branch = typeof(BuildBranch))]
[CliExample("build", "init", "--kit", "cratis-csharp", "--agent", "claude")]
[CliExample("build", "init", "--kit", "https://github.com/acme/kits.git#v1.2.0", "--kit-folder", "node", "--agent", "codex")]
public sealed class BuildInitCommand : AsyncCommand<BuildInitSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, BuildInitSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var project = Path.GetFullPath(settings.Project ?? Directory.GetCurrentDirectory());
        var studio = settings.Studio ?? CliConfiguration.Load().Studio?.Url;
        var scratch = Path.Combine(Path.GetTempPath(), "cratis-build", "kits");
        Directory.CreateDirectory(scratch);
        try
        {
            var fetched = await new BuildKitSource(StudioConnection.Http, studio is null ? null : new Uri(studio), scratch).Fetch(settings.Kit, settings.KitFolder, cancellationToken);
            var record = await new BuildKitInstaller(BuildHostFactory.Log).Install(fetched, project, settings.Agent?.ToLowerInvariant(), settings.Existing, cancellationToken);
            OutputFormatter.WriteMessage(format, $"The kit {record.Kit} {record.Version} is installed ({record.Files.Count} paths, recorded in {BuildProjectSettings.File}). Connect with 'cratis build connect' and build with 'cratis build run'.");
            return ExitCodes.Success;
        }
        catch (InvalidBuildKit invalid)
        {
            OutputFormatter.WriteError(format, $"The kit cannot be installed:{Environment.NewLine}{string.Join(Environment.NewLine, invalid.Problems)}", errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
        catch (HttpRequestException exception)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }
    }
}
