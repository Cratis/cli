// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;

namespace Cratis.Cli.Commands.Ai.Testing;

internal interface IAiTestProcess
{
    Task<AiTestProcessResult> Run(AiTestProcessRequest request, TimeSpan timeout, CancellationToken cancellationToken);
}

internal sealed record AiTestProcessRequest(string Executable, IReadOnlyList<string> Arguments, string Workspace);
internal sealed record AiTestProcessResult(string Status, int? ExitCode, string Output, string? Reason = null, string StandardError = "");

internal sealed class AiTestProcess : IAiTestProcess
{
    public async Task<AiTestProcessResult> Run(AiTestProcessRequest request, TimeSpan timeout, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var process = new Process { StartInfo = StartInfo(request.Executable, request.Arguments, request.Workspace) };
        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            return new("skipped", null, string.Empty, $"Executable unavailable: {request.Executable} ({exception.NativeErrorCode})");
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        using var output = new AiTestProcessOutput(process, deadline.Token);
        var draining = output.Drain();
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            using var grace = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
            grace.CancelAfter(TimeSpan.FromMilliseconds(250));
            await draining.WaitAsync(grace.Token);
            deadline.Token.ThrowIfCancellationRequested();
            return new("completed", process.ExitCode, output.Output, StandardError: output.StandardError);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            return new("error", null, output.Output, Interrupted(deadline, cancellationToken), output.StandardError);
        }
    }

    static string Interrupted(CancellationTokenSource deadline, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return "Cancelled.";
        return deadline.IsCancellationRequested ? "Timeout exceeded; process tree killed." : "Output drain grace exceeded; process-tree termination attempted.";
    }

    static ProcessStartInfo StartInfo(string executable, IReadOnlyList<string> arguments, string workspace)
    {
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workspace,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        return info;
    }

    static void Kill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process exited between cancellation and termination.
        }
    }
}
