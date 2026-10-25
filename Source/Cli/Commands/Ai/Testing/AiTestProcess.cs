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
internal sealed record AiTestProcessResult(string Status, int? ExitCode, string Output, string? Reason = null);

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
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            await process.WaitForExitAsync(CancellationToken.None);
            return new("error", null, await output + await error, cancellationToken.IsCancellationRequested ? "Cancelled." : "Timeout exceeded; process tree killed.");
        }
        return new("completed", process.ExitCode, await output + await error);
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
