// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_CompletionsCommand;

public class when_running_static_completion_with_an_unreachable_server : Specification
{
    int _exitCode;
    string _output;
    string _error;

    async Task Because()
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(typeof(DynamicCompleteCommand).Assembly.Location);
        start.ArgumentList.Add("_complete");
        start.ArgumentList.Add("output-formats");
        start.ArgumentList.Add("--server");
        start.ArgumentList.Add("chronicle://127.0.0.1:1");
        start.ArgumentList.Add("--current");
        start.ArgumentList.Add("j");
        using var process = Process.Start(start)!;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var output = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var error = process.StandardError.ReadToEndAsync(deadline.Token);
        await process.WaitForExitAsync(deadline.Token);
        _exitCode = process.ExitCode;
        _output = await output;
        _error = await error;
    }

    [Fact] void should_exit_without_connecting_to_chronicle() => _exitCode.ShouldEqual(0);
    [Fact] void should_print_only_matching_candidates() => _output.Trim().Split('\n').ShouldContainOnly(["json", "json-compact"]);
    [Fact] void should_not_print_a_banner_or_hint() => _error.ShouldBeEmpty();
}
