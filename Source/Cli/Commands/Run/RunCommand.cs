// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using Cratis.Cli.Commands.Environments;
using Cratis.Cli.Commands.Render;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Environments;
using Cratis.Scene.Engine.Packages;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Runs a Screenplay (.play) file, or the Screenplay files in a folder, in a local Stage sandbox using Docker.
/// </summary>
[LlmDescription("Runs a Screenplay (.play) file, or a folder's Screenplay files, in a local Stage sandbox via Docker. Defaults to the current folder, searched recursively. Only the selected file or folder is mounted, read-only - a file's parent folder never is. Errors before starting Docker for a missing path, a file without the .play extension, a folder without .play files, a path containing a colon, or a ui profile naming a package the Stage neither ships nor was approved to render with --package-set (CLI-RENDER-007). The Stage API (default port 9090) and the Chronicle Workbench (default port 35000) are published on the host. The container's output is hidden while it starts; progress is reported until the Stage answers, and the command keeps running until stopped. A model that declares authentication runs behind AuthProxy: the command resolves the environment (default development, -e or --environment to choose) before starting Docker and exits 5 listing the keys it lacks, never their values; otherwise it starts the Stage on a per-run Docker network with cratis/authproxy in front of it, and AuthProxy (default port 8080) is the address to open. A model that declares onboarding also starts one cratis/ante and its MongoDB, with the model's legal terms files mounted, and writes onboarding emails as .eml files to .cratis/local/mail unless the environment names a mail server. Secrets reach the containers only as files in a temporary folder only you can read, deleted when the command ends. A refused model exits 5 with refusals grouped by kind into Fix your model, Not supported by this engine yet and Ignored by this engine; --verbose lists every refusal, and JSON output carries every issue plus the groups.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("run", "Run a Screenplay (.play) file, or the Screenplay files in a folder, in a local Stage sandbox")]
[CliExample("run")]
[CliExample("run", "./screenplays")]
[CliExample("run", "./screenplays/invoicing.play")]
[CliExample("run", "./screenplays", "--engine", "eventmodel")]
[CliExample("run", "--port", "9191")]
[CliExample("run", "./screenplays", "--sample-data")]
[CliExample("run", "./screenplays", "--sample-data", "demo")]
[CliExample("run", "./screenplays", "--package-set", "./approved-packages.json")]
[CliExample("run", "./screenplays", "--package-set", "./approved-packages.json", "--packages", "./node_modules")]
[CliExample("run", "./screenplays", "-e", "staging")]
[CliExample("run", "./screenplays", "--auth-port", "8090", "--lobby-port", "8091")]
[LlmOption("--tag", "string", "The cratis/stage image tag to run (default: the Stage version this CLI renders with).")]
[LlmOption("--engine", "string", "Stage runtime engine: semantic (default) or eventmodel (explicit fallback), case-insensitive. A refused model exits without switching engines.")]
[LlmOption("--port", "int", "Host port to publish the Stage API on (default: 9090).")]
[LlmOption("--workbench-port", "int", "Host port to publish the Chronicle Workbench on (default: 35000).")]
[LlmOption("--package-set", "string", "Approved package set (JSON) naming custom runtime packages the ui profile may use. The Stage loads them at runtime from their browser builds.")]
[LlmOption("--packages", "string", "Folder holding a folder per approved package, named by its npm name (default: the folder the package set is in).")]
[LlmOption("--environment", "string", "Environment to run with (alias -e): the values for the model's identity providers and mail server. Default: development. Only read when the model declares authentication.")]
[LlmOption("--auth-port", "int", "Host port to publish AuthProxy on when the model declares authentication (default: 8080). This is the application's address.")]
[LlmOption("--lobby-port", "int", "Host port the onboarding lobby is reached on when the model declares onboarding (default: 8081).")]
[LlmOption("--authproxy-tag", "string", "The cratis/authproxy image tag to run when the model declares authentication (default: latest).")]
[LlmOption("--ante-tag", "string", "The ghcr.io/cratis/ante image tag to run when the model declares onboarding (default: 1.6.0).")]
[LlmOption("--verbose", "bool", "Stream the container's output instead of showing startup progress.")]
[LlmOption("--sample-data", "string", "Seed user-authored sample data into the disposable semantic sandbox. Optional PACK overrides profile bindings and active pack; omitted PACK uses them. Scalar projections only; collection properties remain empty (Cratis/Screenplay#1000). Does not change rendered production applications.")]
public class RunCommand : AsyncCommand<RunSettings>
{
    /// <summary>
    /// How long to wait for the container to go away on its own after the command is interrupted, before
    /// stopping it explicitly.
    /// </summary>
    static readonly TimeSpan _stopGrace = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long to give Docker to stop and remove the container after the command is interrupted.
    /// </summary>
    static readonly TimeSpan _stopTimeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var input = RunInput.Resolve(settings.Path ?? Directory.GetCurrentDirectory());

        if (input.Target is not { } target)
        {
            OutputFormatter.WriteError(format, input.Error!, "Pass a .play file or a folder that contains one or more .play files, or run this command from such a folder", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        if (!SampleEngineSupported(settings, format)) return ExitCodes.ValidationError;

        var (approved, packages, packageExitCode) = await ResolvePackages(settings, format, cancellationToken);
        if (packageExitCode != ExitCodes.Success) return packageExitCode;

        // The profile is resolved before Docker starts: a package neither shipped nor approved would otherwise start a
        // container that renders a placeholder where the package's component belongs.
        var (findings, application, scene) = RunProfileCheck.CompileAndCheck(target, approved, settings.SampleData.IsSet);
        if (findings.Count > 0)
        {
            WriteProfileRefusal(format, findings);
            return ExitCodes.ValidationError;
        }

        var (seedPlay, seedExitCode) = PrepareSeeds(new(target, settings, format), application, scene);
        if (seedExitCode != ExitCodes.Success) return seedExitCode;

        // A model with authentication runs behind AuthProxy, with the values of an environment. Those are resolved, and their absence reported, before Docker starts.
        var stageName = StageContainer.GenerateName();
        var launch = new RunLaunch(target, settings, format) { Packages = packages, Name = stageName };
        var stackPreparation = await PrepareStack(launch, application, cancellationToken);
        if (stackPreparation.ExitCode != ExitCodes.Success) return stackPreparation.ExitCode;

        launch = launch with
        {
            Stack = stackPreparation.Stack,
            RunDirectory = stackPreparation.Directory?.FullName,
            Endpoints = StageEndpoints.For(settings.Port, settings.WorkbenchPort)
        };
        RunOutput.WriteHeader(format, target.FullName, launch.Endpoints, launch.Stack);
        using var interrupt = ShutdownSignal.LinkedTo(cancellationToken);
        return await RunWithSeeds(launch, seedPlay, interrupt.Token);
    }

    static bool SampleEngineSupported(RunSettings settings, string format)
    {
        if (!settings.SampleData.IsSet || settings.Engine != "eventmodel") return true;
        OutputFormatter.WriteError(format, "--sample-data requires --engine semantic; the eventmodel fallback cannot apply sandbox seeds", "Use --engine semantic", ExitCodes.ValidationErrorCode);
        return false;
    }

    static async Task<(ApprovedPackageSet Approved, StagePackageMounts? Mounts, int ExitCode)> ResolvePackages(RunSettings settings, string format, CancellationToken cancellationToken)
    {
        var approved = ApprovedPackageSet.Empty;
        StagePackageMounts? packages = null;
        if (settings.PackageSet is null && settings.Packages is not null)
        {
            OutputFormatter.WriteError(format, "--packages needs --package-set", "Pass the approved package set document the packages are listed in", ExitCodes.ValidationErrorCode);
            return (approved, packages, ExitCodes.ValidationError);
        }

        if (settings.PackageSet is not null)
        {
            var setFile = Path.GetFullPath(settings.PackageSet);
            var read = await ApprovedPackageSetFile.Read(setFile, cancellationToken);
            if (read.Error is { } error)
            {
                OutputFormatter.WriteError(format, error, ApprovedPackageSetFile.Hint, read.NotFound ? ExitCodes.NotFoundCode : ExitCodes.ValidationErrorCode);
                return (approved, packages, read.NotFound ? ExitCodes.NotFound : ExitCodes.ValidationError);
            }

            approved = read.Set!;
            var resolved = StagePackageMounts.Resolve(setFile, approved, settings.Packages);
            if (resolved.Error is { } mountError)
            {
                OutputFormatter.WriteError(format, mountError, "Point --packages at the folder that holds a folder for each approved package, named by its npm name", ExitCodes.ValidationErrorCode);
                return (approved, packages, ExitCodes.ValidationError);
            }

            packages = resolved.Mounts;
        }

        return (approved, packages, ExitCodes.Success);
    }

    static void WriteProfileRefusal(string format, IReadOnlyList<ScreenplayDiagnostic> findings)
    {
        ScreenplayDiagnosticsWriter.Write(format, findings);
        var unknownPackage = findings.Any(_ => string.Equals(_.Code, SceneProfileResolution.UnknownPackageCode, StringComparison.Ordinal));
        OutputFormatter.WriteError(
            format,
            unknownPackage
                ? $"Nothing was started - the ui profile asks for something the Stage cannot render ({findings.Count} error(s))"
                : $"Nothing was started - the application shell does not compile ({findings.Count} error(s))",
            unknownPackage
                ? "Approve a custom runtime package with --package-set (and --packages for its browser build), or use only core, scene.web and Cratis.Components"
                : "Fix the navigation, toolbar or sample data reported above; `cratis screenplay validate` reports the same codes",
            ExitCodes.ValidationErrorCode);
    }

    static (string? SeedPlay, int ExitCode) PrepareSeeds(RunLaunch launch, ApplicationSyntax? application, SceneApplication? scene)
    {
        var settings = launch.Settings;
        var format = launch.Format;
        var target = launch.Target;
        string? seedPlay = null;
        if (settings.SampleData.IsSet)
        {
            if (application is null || scene is null)
            {
                OutputFormatter.WriteError(format, "Sample data requires a model that compiles", "Run cratis screenplay validate on the model", ExitCodes.ValidationErrorCode);
                return (null, ExitCodes.ValidationError);
            }

            var pack = string.IsNullOrWhiteSpace(settings.SampleData.Value) ? null : settings.SampleData.Value;
            var seeds = SampleDataSeeds.Create(application, scene, pack);
            if (seeds.Error is not null)
            {
                OutputFormatter.WriteError(format, seeds.Error, "Choose a user-authored sample data pack", ExitCodes.ValidationErrorCode);
                return (null, ExitCodes.ValidationError);
            }

            ScreenplayDiagnosticsWriter.Write(format, seeds.Warnings.Select(warning => new ScreenplayDiagnostic(
                ScreenplayDiagnosticSeverity.Warning,
                "CLI-RUN-SAMPLE-DATA",
                warning.Message,
                warning.Location is { } location ? $"{location.Path ?? target.FullName}({location.Line},{location.Column})" : null)));
            seedPlay = seeds.SeedPlay;
        }

        return (seedPlay, ExitCodes.Success);
    }

    static async Task<StackPreparation> PrepareStack(RunLaunch launch, ApplicationSyntax? application, CancellationToken cancellationToken)
    {
        var target = launch.Target;
        var settings = launch.Settings;
        var format = launch.Format;
        if (application?.Authentication is null) return new(null, null, ExitCodes.Success);

        RunEnvironment? environment = null;
        var environmentResult = await EnvironmentCommandSupport.Run(format, async () =>
        {
            environment = await RunEnvironment.Resolve(application, ModelFolder(target), settings.SelectedEnvironmentName ?? settings.EnvironmentShort, EnvironmentCommandSupport.Passphrase(creating: false), cancellationToken);
            return ExitCodes.Success;
        });
        if (environmentResult != ExitCodes.Success)
        {
            return new(null, null, environmentResult);
        }

        if (environment!.Missing.Count > 0)
        {
            RunOutput.WriteMissingKeys(format, environment, target.FullName);
            return new(null, null, ExitCodes.ValidationError);
        }

        return PlanStack(launch, application, environment);
    }

    static StackPreparation PlanStack(RunLaunch launch, ApplicationSyntax application, RunEnvironment environment)
    {
        DirectoryInfo? runDirectory = null;
        try
        {
            runDirectory = RunStackFiles.CreateRunDirectory();
            var mailFolder = Path.Combine(environment.ProjectRoot, ".cratis", "local", "mail");
            var stackInput = new RunStackInput(application, environment, launch.Settings, ModelFolder(launch.Target), runDirectory.FullName, launch.Name!, mailFolder, AttestationKey.Generate(launch.Name!), HostUser.Current());
            var stack = RunStackPlanner.Plan(stackInput);
            if (stack.MailFolder is not null)
            {
                RunStackFiles.CreateMailFolder(environment.ProjectRoot);
            }

            return new(stack, runDirectory, ExitCodes.Success);
        }
        catch (InvalidOperationException exception)
        {
            if (runDirectory is not null) RunStackFiles.Delete(runDirectory.FullName);
            OutputFormatter.WriteError(launch.Format, exception.Message, "Nothing was started - fix the model's file references", ExitCodes.ValidationErrorCode);
            return new(null, null, ExitCodes.ValidationError);
        }
    }

    static async Task<int> RunWithSeeds(RunLaunch launch, string? seedPlay, CancellationToken cancellationToken)
    {
        DirectoryInfo? seedDirectory = null;
        try
        {
            string? sampleDataFile = null;
            if (seedPlay is not null)
            {
                seedDirectory = Directory.CreateTempSubdirectory("cratis-run-");
                sampleDataFile = Path.Combine(seedDirectory.FullName, "cratis-sample-data.play");
                await File.WriteAllTextAsync(sampleDataFile, seedPlay, cancellationToken);
            }

            return await RunStage(launch with { SampleDataFile = sampleDataFile }, cancellationToken);
        }
        finally
        {
            seedDirectory?.Delete(true);
            if (launch.RunDirectory is not null) RunStackFiles.Delete(launch.RunDirectory);
        }
    }

    static async Task<int> RunStage(RunLaunch launch, CancellationToken cancellationToken)
    {
        var settings = launch.Settings;
        var format = launch.Format;
        var stack = launch.Stack;
        if (cancellationToken.IsCancellationRequested) return ExitCodes.Success;

        var (host, hostExitCode) = await PrepareHost(launch, cancellationToken);
        if (hostExitCode != ExitCodes.Success) return hostExitCode;

        using var session = Start(launch);
        if (session is null)
        {
            if (host is not null) await host.Stop(null);
            OutputFormatter.WriteError(format, "Failed to start Docker", "Ensure Docker is installed and the 'docker' command is on your PATH", ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }

        try
        {
            var startupExitCode = await StartStage(session, host, launch, cancellationToken);
            if (startupExitCode.HasValue) return startupExitCode.Value;

            RunOutput.WriteReady(format, session.Startup, launch.Target.FullName, launch.Endpoints, session.RuntimeStatus?.Engine, session.RuntimeStatus?.Notes, settings.Verbose, stack);
            return await WaitForRunExit(session, host, format, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await HandleCancellation(session, host, format);
            return ExitCodes.Success;
        }
        finally
        {
            // Everything the run created goes with it: the containers, the network and the files that held the environment's values.
            if (host is not null) await host.Stop(session);
        }
    }

    static async Task HandleCancellation(StageSession session, RunStackHost? host, string format)
    {
        // A Ctrl+C from a terminal reaches the Docker client too, and stops and removes the container on
        // its own more often than not - but a plain kill only reaches this process, and even a Ctrl+C races
        // the client's own teardown, so what follows confirms the container rather than assuming it.
        RunOutput.WriteStopping(format);
        if (host is null && !await WaitForStop(session))
        {
            RunOutput.WriteStopTimedOut(format, _stopTimeout);
        }
    }

    static async Task<(RunStackHost? Host, int ExitCode)> PrepareHost(RunLaunch launch, CancellationToken cancellationToken)
    {
        if (launch.Stack is null) return (null, ExitCodes.Success);
        var (prepared, error) = await RunStackHost.Prepare(launch.Stack, launch.RunDirectory!, cancellationToken);
        if (prepared is not null) return (prepared, ExitCodes.Success);
        OutputFormatter.WriteError(launch.Format, error!, "Ensure Docker is installed, running and the 'docker' command is on your PATH", ExitCodes.ConnectionErrorCode);
        return (null, ExitCodes.ConnectionError);
    }

    static async Task<int?> StartStage(StageSession session, RunStackHost? host, RunLaunch launch, CancellationToken cancellationToken)
    {
        if (!await WaitUntilReady(session, launch.Settings, launch.Format, cancellationToken))
        {
            return await ReportStartupFailure(session, launch);
        }

        if (host is not null && !await StartStack(host, launch.Settings, launch.Format, cancellationToken))
        {
            return ExitCodes.ServerError;
        }

        return null;
    }

    static async Task<int> ReportStartupFailure(StageSession session, RunLaunch launch)
    {
        if (session.RuntimeStatus is { IsUnsupported: true } refused)
        {
            RunOutput.WriteRefusal(launch.Format, refused, launch.Settings);
            if (!await WaitForStop(session)) RunOutput.WriteStopTimedOut(launch.Format, _stopTimeout);
            return ExitCodes.ValidationError;
        }

        // The container exited on its own - wait for the process so all of its output is captured,
        // then show it, since it was hidden while starting.
        await session.WaitForExit(CancellationToken.None);
        RunOutput.WriteFailure(launch.Format, session);
        return ExitCodes.ServerError;
    }

    static async Task<int> WaitForRunExit(StageSession session, RunStackHost? host, string format, CancellationToken cancellationToken)
    {
        if (host is null)
        {
            await session.WaitForExit(cancellationToken);
            return ExitCodeFor(session);
        }

        await Task.WhenAny(session.WaitForExit(cancellationToken), host.WaitForAnyExit(cancellationToken));
        if (session.HasExited)
        {
            return ExitCodeFor(session);
        }

        RunOutput.WriteStackFailure(format, host);
        return ExitCodes.ServerError;
    }

    static async Task<bool> StartStack(RunStackHost host, RunSettings settings, string format, CancellationToken cancellationToken)
    {
        var echo = settings.Verbose && !string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) && !string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal);
        if (host.Start(echo, capture: true) && await WaitUntilStackReady(host, settings, format, cancellationToken))
        {
            return true;
        }

        RunOutput.WriteStackFailure(format, host);
        return false;
    }

    static async Task<bool> WaitUntilStackReady(RunStackHost host, RunSettings settings, string format, CancellationToken cancellationToken)
    {
        if (settings.Verbose || !string.Equals(format, OutputFormats.Table, StringComparison.Ordinal))
        {
            return await host.WaitUntilReady(() => { }, cancellationToken);
        }

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(new Style(OutputFormatter.Accent))
            .StartAsync("Starting AuthProxy...", async _ => await host.WaitUntilReady(() => { }, cancellationToken));
    }

    static string ModelFolder(FileSystemInfo target) => target is DirectoryInfo directory ? directory.FullName : Path.GetDirectoryName(target.FullName)!;

    static StageSession? Start(RunLaunch launch)
    {
        var settings = launch.Settings;
        var name = launch.Name ?? StageContainer.GenerateName();
        var arguments = launch.Target is FileInfo
            ? StageContainer.BuildRunArgumentsForFile(launch.Target.FullName, settings.Tag, settings.Port, settings.WorkbenchPort, name, launch.Packages, launch.Engine, launch.SampleDataFile, launch.Stack?.Stage)
            : StageContainer.BuildRunArguments(launch.Target.FullName, settings.Tag, settings.Port, settings.WorkbenchPort, name, launch.Packages, launch.Engine, launch.SampleDataFile, launch.Stack?.Stage);
        arguments = StageContainer.BindStandalonePorts(arguments, settings.AllowRemote, launch.Stack?.Stage);

        try
        {
            var format = settings.ResolveOutputFormat();
            var captureOutput = !settings.Verbose || string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal);
            return StageSession.Start(name, $"{StageContainer.Image}:{settings.Tag}", arguments, captureOutput);
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    static async Task<bool> WaitUntilReady(StageSession session, RunSettings settings, string format, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();

        if (settings.Verbose || !string.Equals(format, OutputFormats.Table, StringComparison.Ordinal))
        {
            return await session.WaitUntilReady(settings.Port, () => { }, cancellationToken);
        }

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(new Style(OutputFormatter.Accent))
            .StartAsync(
                RunOutput.StatusText(session.Startup, TimeSpan.Zero),
                async ctx => await session.WaitUntilReady(
                    settings.Port,
                    () => ctx.Status(RunOutput.StatusText(session.Startup, Stopwatch.GetElapsedTime(started))),
                    cancellationToken));
    }

    static async Task<bool> WaitForStop(StageSession session)
    {
        // A Ctrl+C from a terminal reaches the Docker client too, so give that a moment to take the container
        // down on its own before asking Docker to do it.
        await Exits(session, _stopGrace);

        // Asked unconditionally, because the client exiting does not mean the container did. It is a different
        // process from the container it started, and it can go away while the sandbox keeps running - which is
        // how a run could report a clean stop and leave a container up, holding the port the next run wants.
        // Stopping one that is already gone is not an error.
        await session.Stop();

        return await StoppedWithin(session, _stopTimeout - _stopGrace);
    }

    /// <summary>
    /// Waits until Docker says the container is gone.
    /// </summary>
    /// <param name="session">The session to wait on.</param>
    /// <param name="within">How long to wait.</param>
    /// <returns>True when the container went away in time.</returns>
    static async Task<bool> StoppedWithin(StageSession session, TimeSpan within)
    {
        var deadline = Stopwatch.GetTimestamp();

        while (Stopwatch.GetElapsedTime(deadline) < within)
        {
            if (!await session.IsRunning())
            {
                return true;
            }

            await Task.Delay(StageReadiness.PollInterval, CancellationToken.None);
        }

        return !await session.IsRunning();
    }

    static async Task<bool> Exits(StageSession session, TimeSpan within)
    {
        try
        {
            await session.WaitForExit(CancellationToken.None).WaitAsync(within, CancellationToken.None);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    static int ExitCodeFor(StageSession session) => session.ExitCode == 0 ? ExitCodes.Success : ExitCodes.ServerError;

    sealed record StackPreparation(RunStackPlan? Stack, DirectoryInfo? Directory, int ExitCode);

    sealed record RunLaunch(FileSystemInfo Target, RunSettings Settings, string Format)
    {
        public StagePackageMounts? Packages { get; init; }
        public StageEndpoints Endpoints { get; init; } = null!;
        public string Engine { get; } = Settings.Engine ?? "semantic";
        public string? SampleDataFile { get; init; }
        public RunStackPlan? Stack { get; init; }
        public string? RunDirectory { get; init; }
        public string? Name { get; init; }
    }
}
