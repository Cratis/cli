// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Templating.PostActions;

/// <summary>
/// Interactive confirmation hook for script actions under the prompt policy. The CLI assigns the
/// callback; the engine itself never reads the console.
/// </summary>
public static class PromptCallback
{
    /// <summary>
    /// Gets or sets the confirmation callback used by the prompt policy; returning true runs the script.
    /// </summary>
    public static Func<Configuration.PostActionConfig, string, Task<bool>>? ConfirmAction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an interactive terminal is attached.
    /// </summary>
    public static bool IsInteractive { get; set; }

    /// <summary>
    /// Confirms a script action through the callback, defaulting to decline when none is assigned.
    /// </summary>
    /// <param name="action">The action asking for confirmation.</param>
    /// <param name="executable">The executable that would run.</param>
    /// <returns>True when the person allowed the script.</returns>
    public static Task<bool> Confirm(Configuration.PostActionConfig action, string executable) =>
        ConfirmAction is null ? Task.FromResult(false) : ConfirmAction(action, executable);
}

/// <summary>
/// Runs scripts as post actions. The script policy is explicit: <c language="csharp">Allow</c> runs, <c language="csharp">Deny</c> reports
/// the action as declined, and <c language="csharp">Prompt</c> requires an interactive confirmation delegated through the
/// provided callback — a non-interactive run without an explicit policy is a failure, never a silent
/// run and never a silent skip.
/// </summary>
static class RunScript
{
    public static async Task<PostActionResult> Run(
        Configuration.PostActionConfig action,
        InstantiationResult result,
        ScriptPolicy policy,
        CancellationToken cancellationToken)
    {
        var executable = action.Args.GetValueOrDefault("executable")
            ?? throw new InvalidTemplateManifest($"post action '{action.ActionId}' is missing arg 'executable'.");

        switch (policy)
        {
            case ScriptPolicy.Deny:
                return new PostActionResult(
                    action, PostActionOutcome.Denied, $"declined to run '{executable}' (--allow-scripts no).", PostActionRunner.JoinInstructions(action));

            case ScriptPolicy.Prompt when !PromptCallback.IsInteractive:
                return new PostActionResult(
                    action,
                    PostActionOutcome.Failed,
                    "script actions require an explicit --allow-scripts yes|no when stdin is not a TTY.",
                    PostActionRunner.JoinInstructions(action));

            case ScriptPolicy.Prompt when !await PromptCallback.Confirm(action, executable):
                return new PostActionResult(
                    action, PostActionOutcome.Denied, "declined at the prompt.", PostActionRunner.JoinInstructions(action));
        }

        var arguments = action.Args.GetValueOrDefault("args") ?? string.Empty;
        var workingDirectory = action.Args.GetValueOrDefault("workingDirectory") is { } directory
            ? Path.Combine(result.OutputRoot, directory)
            : result.OutputRoot;

        var startInfo = new ProcessStartInfo(executable, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(startInfo)
            ?? throw new TemplatePackageAcquisitionError($"failed to start '{executable}'.");
        await process.WaitForExitAsync(cancellationToken);

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        return process.ExitCode == 0
            ? new PostActionResult(action, PostActionOutcome.Succeeded, $"ran '{executable} {arguments}'.{Tail(output)}", string.Empty)
            : new PostActionResult(
                action,
                PostActionOutcome.Failed,
                $"'{executable} {arguments}' exited with {process.ExitCode}.{Tail(error.Length > 0 ? error : output)}",
                PostActionRunner.JoinInstructions(action));
    }

    static string Tail(string output) => string.IsNullOrWhiteSpace(output) ? string.Empty : $"\n{output.TrimEnd()}";
}

/// <summary>
/// The single toolchain-dependent action: restore runs <c language="csharp">dotnet restore</c> when dotnet is on PATH;
/// when it is not, the action is reported as not performed with its manual instructions — a scaffold
/// that produced correct sources but could not restore is a different outcome from a failed one.
/// </summary>
static class Restore
{
    public static async Task<PostActionResult> Run(
        Configuration.PostActionConfig action,
        InstantiationResult result,
        CancellationToken cancellationToken)
    {
        if (!IsDotnetOnPath())
        {
            return new PostActionResult(
                action,
                PostActionOutcome.NotPerformed,
                "dotnet was not found on PATH — run 'dotnet restore' yourself. The scaffold itself completed.",
                PostActionRunner.JoinInstructions(action));
        }

        var projects = result.PrimaryOutputs
            .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
            .DefaultIfEmpty(Path.Combine(result.OutputRoot, $"{result.Name}.csproj"))
            .Where(File.Exists)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var project in projects)
        {
            var startInfo = new ProcessStartInfo("dotnet", $"restore \"{project}\"")
            {
                WorkingDirectory = result.OutputRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new PostActionResult(action, PostActionOutcome.Failed, "failed to start dotnet restore.", PostActionRunner.JoinInstructions(action));
            }
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                return new PostActionResult(action, PostActionOutcome.Failed, $"dotnet restore exited with {process.ExitCode}: {error.Trim()}", PostActionRunner.JoinInstructions(action));
            }
        }

        return new PostActionResult(action, PostActionOutcome.Succeeded, $"restored {projects.Length} project(s).", string.Empty);
    }

    internal static bool IsDotnetOnPath()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathVariable.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }
            try
            {
                var executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
                if (File.Exists(Path.Combine(directory.Trim(), executable)))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                // Unreadable PATH entries are skipped.
            }
        }
        return false;
    }
}
