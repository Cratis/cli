// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using Cratis.Cli.Commands.Render;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Runs a Screenplay (.play) file, or the Screenplay files in a folder, in a local Stage sandbox using Docker.
/// </summary>
[LlmDescription("Runs a Screenplay (.play) file, or a folder's Screenplay files, in a local Stage sandbox via Docker. Defaults to the current folder, searched recursively. Only the selected file or folder is mounted, read-only - a file's parent folder never is. Errors before starting Docker for a missing path, a file without the .play extension, a folder without .play files, a path containing a colon, or a ui profile naming a package the Stage neither ships nor was approved to render with --package-set (CLI-RENDER-007). The Stage API (default port 9090) and the Chronicle Workbench (default port 35000) are published on the host. The container's output is hidden while it starts; progress is reported until the Stage answers, and the command keeps running until stopped. A refused model exits 5 with refusals grouped by kind into Fix your model, Not supported by this engine yet and Ignored by this engine; --verbose lists every refusal, and JSON output carries every issue plus the groups.")]
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
[LlmOption("--tag", "string", "The cratis/stage image tag to run (default: the Stage version this CLI renders with).")]
[LlmOption("--engine", "string", "Stage runtime engine: semantic (default) or eventmodel (explicit fallback), case-insensitive. A refused model exits without switching engines.")]
[LlmOption("--port", "int", "Host port to publish the Stage API on (default: 9090).")]
[LlmOption("--workbench-port", "int", "Host port to publish the Chronicle Workbench on (default: 35000).")]
[LlmOption("--package-set", "string", "Approved package set (JSON) naming custom runtime packages the ui profile may use. The Stage loads them at runtime from their browser builds.")]
[LlmOption("--packages", "string", "Folder holding a folder per approved package, named by its npm name (default: the folder the package set is in).")]
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

        if (settings.SampleData.IsSet && settings.Engine == "eventmodel")
        {
            OutputFormatter.WriteError(format, "--sample-data requires --engine semantic; the eventmodel fallback cannot apply sandbox seeds", "Use --engine semantic", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        var approved = ApprovedPackageSet.Empty;
        StagePackageMounts? packages = null;
        if (settings.PackageSet is null && settings.Packages is not null)
        {
            OutputFormatter.WriteError(format, "--packages needs --package-set", "Pass the approved package set document the packages are listed in", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        if (settings.PackageSet is not null)
        {
            var setFile = Path.GetFullPath(settings.PackageSet);
            var read = await ApprovedPackageSetFile.Read(setFile, cancellationToken);
            if (read.Error is { } error)
            {
                OutputFormatter.WriteError(format, error, ApprovedPackageSetFile.Hint, read.NotFound ? ExitCodes.NotFoundCode : ExitCodes.ValidationErrorCode);
                return read.NotFound ? ExitCodes.NotFound : ExitCodes.ValidationError;
            }

            approved = read.Set!;
            var resolved = StagePackageMounts.Resolve(setFile, approved, settings.Packages);
            if (resolved.Error is { } mountError)
            {
                OutputFormatter.WriteError(format, mountError, "Point --packages at the folder that holds a folder for each approved package, named by its npm name", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            packages = resolved.Mounts;
        }

        // The profile is resolved before Docker starts: a package neither shipped nor approved would otherwise start a
        // container that renders a placeholder where the package's component belongs.
        var (findings, application, scene) = RunProfileCheck.CompileAndCheck(target, approved, settings.SampleData.IsSet);
        if (findings.Count > 0)
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
            return ExitCodes.ValidationError;
        }

        string? seedPlay = null;
        if (settings.SampleData.IsSet)
        {
            if (application is null || scene is null)
            {
                OutputFormatter.WriteError(format, "Sample data requires a model that compiles", "Run cratis screenplay validate on the model", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            var pack = string.IsNullOrWhiteSpace(settings.SampleData.Value) ? null : settings.SampleData.Value;
            var seeds = SampleDataSeeds.Create(application, scene, pack);
            if (seeds.Error is not null)
            {
                OutputFormatter.WriteError(format, seeds.Error, "Choose a user-authored sample data pack", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            ScreenplayDiagnosticsWriter.Write(format, seeds.Warnings.Select(warning => new ScreenplayDiagnostic(
                ScreenplayDiagnosticSeverity.Warning, "CLI-RUN-SAMPLE-DATA", warning.Message,
                warning.Location is { } location ? $"{location.Path ?? target.FullName}({location.Line},{location.Column})" : null)));
            seedPlay = seeds.SeedPlay;
        }

        using var overlay = seedPlay is not null ? new SampleDataSeedOverlay(application!, seedPlay) : null;
        var path = target.FullName;
        var endpoints = StageEndpoints.For(settings.Port, settings.WorkbenchPort);
        RunOutput.WriteHeader(format, path, endpoints);

        var engine = settings.Engine ?? "semantic";
        using var interrupt = ShutdownSignal.LinkedTo(cancellationToken);

        return await RunStage(target, settings, packages, format, endpoints, engine, interrupt.Token, overlay is null ? null : new DirectoryInfo(overlay.Path));
    }

    static async Task<int> RunStage(
        FileSystemInfo target,
        RunSettings settings,
        StagePackageMounts? packages,
        string format,
        StageEndpoints endpoints,
        string engine,
        CancellationToken cancellationToken,
        FileSystemInfo? mountedInput = null)
    {
        if (cancellationToken.IsCancellationRequested) return ExitCodes.Success;
        using var session = Start(mountedInput ?? target, settings, packages, engine);
        if (session is null)
        {
            OutputFormatter.WriteError(format, "Failed to start Docker", "Ensure Docker is installed and the 'docker' command is on your PATH", ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }

        try
        {
            if (!await WaitUntilReady(session, settings, format, cancellationToken))
            {
                if (session.RuntimeStatus is { IsUnsupported: true } refused)
                {
                    RunOutput.WriteRefusal(format, refused, settings);
                    if (!await WaitForStop(session)) RunOutput.WriteStopTimedOut(format, _stopTimeout);
                    return ExitCodes.ValidationError;
                }

                // The container exited on its own - wait for the process so all of its output is captured,
                // then show it, since it was hidden while starting.
                await session.WaitForExit(CancellationToken.None);
                RunOutput.WriteFailure(format, session);
                return ExitCodes.ServerError;
            }

            RunOutput.WriteReady(format, session.Startup, target.FullName, endpoints, session.RuntimeStatus?.Engine, session.RuntimeStatus?.Notes, settings.Verbose);
            await session.WaitForExit(cancellationToken);

            return ExitCodeFor(session);
        }
        catch (OperationCanceledException)
        {
            // A Ctrl+C from a terminal reaches the Docker client too, and stops and removes the container on
            // its own more often than not - but a plain kill only reaches this process, and even a Ctrl+C races
            // the client's own teardown, so what follows confirms the container rather than assuming it.
            RunOutput.WriteStopping(format);
            if (!await WaitForStop(session))
            {
                RunOutput.WriteStopTimedOut(format, _stopTimeout);
            }

            return ExitCodes.Success;
        }
    }

    static StageSession? Start(FileSystemInfo input, RunSettings settings, StagePackageMounts? packages, string engine)
    {
        var name = StageContainer.GenerateName();
        var arguments = input is FileInfo
            ? StageContainer.BuildRunArgumentsForFile(input.FullName, settings.Tag, settings.Port, settings.WorkbenchPort, name, packages, engine)
            : StageContainer.BuildRunArguments(input.FullName, settings.Tag, settings.Port, settings.WorkbenchPort, name, packages, engine);

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
}
