// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand;

public class when_showing_help : Specification
{
    int _exitCode;
    string _output;

    async Task Because()
    {
        var specs = GetType().Assembly.Location;
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        string[] arguments = ["exec", "--runtimeconfig", Path.ChangeExtension(specs, ".runtimeconfig.json"), "--depsfile", Path.ChangeExtension(specs, ".deps.json"), typeof(DirectMcpCommand).Assembly.Location,
            "direct", "mcp", "--help"];
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var child = new Process { StartInfo = start };
        child.Start().ShouldBeTrue();
        var output = child.StandardOutput.ReadToEndAsync();
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await child.WaitForExitAsync(deadline.Token);
            _exitCode = child.ExitCode;
            _output = await output;
        }
        finally
        {
            if (!child.HasExited) child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync();
            await Task.WhenAll(output, errors);
        }
    }

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_show_the_no_tenant_option() => _output.ShouldContain("--no-tenant");
}
