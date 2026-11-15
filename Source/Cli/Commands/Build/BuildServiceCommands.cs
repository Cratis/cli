// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Installs the build loop of a project as a user service that starts at sign-in and restarts when it stops.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand(
    "install",
    "Run the build loop of a project as an always-on user service (launchd on macOS, systemd on Linux) that starts at sign-in and restarts when it stops. Takes the options of 'cratis build run'; installing again replaces the service.",
    Branch = typeof(BuildBranch.Service))]
[CliExample("build", "service", "install", "--agent", "claude")]
[CliExample("build", "service", "install", "--project", "~/src/shop", "--exec", "./generate.sh", "--no-push")]
public sealed class BuildServiceInstallCommand : AsyncCommand<BuildServiceSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, BuildServiceSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        if (CliConfiguration.Load().Studio?.BuilderId is null)
        {
            OutputFormatter.WriteError(format, "This machine is not registered as a builder.", "Run 'cratis build connect --studio <url> --organization <name>' first.", ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        if (settings.Kit is null && settings.Exec is null && BuildProjectSettings.Read(settings.ProjectPath) is null)
        {
            OutputFormatter.WriteError(format, $"There is no kit in {settings.ProjectPath}.", "Run 'cratis build init --kit <kit>' there, or pass --kit or --exec.", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        return await BuildServiceCommand.With(format, Install, cancellationToken);

        async Task<int> Install(BuildServices services)
        {
            var (program, prefix) = BuildServicePlatforms.Program(Environment.ProcessPath!, Assembly.GetEntryAssembly()!.Location);
            var definition = BuildServiceDefinition.For(settings, program, prefix, Environment.GetEnvironmentVariable("PATH") ?? string.Empty);
            var state = await services.Install(definition, cancellationToken);
            OutputFormatter.WriteMessage(format, $"The builder of {definition.Project} runs as the {state.Manager} service {state.Name} ({(state.Running ? "running" : "starting")}). Its output goes to {state.Logs}.");
            return ExitCodes.Success;
        }
    }
}

/// <summary>
/// Stops and removes the user service of a project's build loop.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand(
    "uninstall",
    "Stop and remove the user service that runs the build loop of a project.",
    Branch = typeof(BuildBranch.Service))]
[CliExample("build", "service", "uninstall")]
public sealed class BuildServiceUninstallCommand : AsyncCommand<BuildServiceSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, BuildServiceSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var name = BuildServiceDefinition.NameFor(settings.ProjectPath);
        return BuildServiceCommand.With(format, Uninstall, cancellationToken);

        async Task<int> Uninstall(BuildServices services)
        {
            var removed = await services.Uninstall(name, cancellationToken);
            OutputFormatter.WriteMessage(format, removed ? $"Removed the service {name}." : $"There is no builder service for {settings.ProjectPath}.");
            return ExitCodes.Success;
        }
    }
}

/// <summary>
/// Reports whether a project's build loop is installed as a user service and running.
/// </summary>
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand(
    "status",
    "Show whether the build loop of a project is installed as a user service, whether it is running, and where its output goes. Changes nothing.",
    Branch = typeof(BuildBranch.Service))]
[CliExample("build", "service", "status")]
[CliExample("build", "service", "status", "--output", "json")]
public sealed class BuildServiceStatusCommand : AsyncCommand<BuildServiceSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, BuildServiceSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var name = BuildServiceDefinition.NameFor(settings.ProjectPath);
        return BuildServiceCommand.With(format, Report, cancellationToken);

        async Task<int> Report(BuildServices services)
        {
            OutputFormatter.WriteObject(format, await services.Status(name, cancellationToken), Render);
            return ExitCodes.Success;
        }
    }

    static void Render(BuildServiceState state)
    {
        OutputFormatter.WriteLabel("Service", $"{state.Name} ({state.Manager})");
        OutputFormatter.WriteLabel("Installed", state.Installed ? state.Unit : "no; run 'cratis build service install'");
        OutputFormatter.WriteLabel("Running", state.Running ? "yes" : "no");
        OutputFormatter.WriteLabel("Output", state.Logs);
    }
}

static class BuildServiceCommand
{
    public static async Task<int> With(string format, Func<BuildServices, Task<int>> action, CancellationToken cancellationToken)
    {
        var platform = await BuildServicePlatforms.Current(cancellationToken);
        if (platform is null)
        {
            OutputFormatter.WriteError(format, "This operating system has no user service manager the builder supports.", "Use launchd on macOS or systemd on Linux, or run 'cratis build run' yourself.", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        try
        {
            return await action(new BuildServices(platform, BuildServicePlatforms.Run));
        }
        catch (BuildServiceFailed failed)
        {
            OutputFormatter.WriteError(format, failed.Message, errorCode: ExitCodes.ServerErrorCode);
            return ExitCodes.ServerError;
        }
    }
}
