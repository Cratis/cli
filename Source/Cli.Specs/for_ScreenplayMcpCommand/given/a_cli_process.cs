// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_ScreenplayMcpCommand.given;

public class a_cli_process : Specification
{
    protected int _exitCode;
    protected string _output;
    protected string _error;

    protected async Task Run(params string[] args)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // The specs exercise command routing, not release lookups; keep them off the network and out of the version cache.
        start.Environment[UpdateChecker.DisableEnvVar] = "1";

        // CI terminals get colored output, and color codes split the text the specs look for.
        start.Environment["NO_COLOR"] = "1";
        start.ArgumentList.Add(typeof(ScreenplayMcpCommand).Assembly.Location);
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var output = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var error = process.StandardError.ReadToEndAsync(deadline.Token);
        await process.WaitForExitAsync(deadline.Token);
        _exitCode = process.ExitCode;
        _output = await output;
        _error = await error;
    }
}
