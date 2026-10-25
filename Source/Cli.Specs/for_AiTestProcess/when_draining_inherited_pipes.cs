// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestProcess;

public class when_draining_inherited_pipes : Specification
{
    readonly List<int> _children = [];
    AiTestProcessResult _grace = null!;
    AiTestProcessResult _deadline = null!;
    AiTestProcessResult _cancelled = null!;
    AiTestProcessResult _large = null!;

    public static bool NonWindows => !OperatingSystem.IsWindows();

    async Task Because()
    {
        var runner = new AiTestProcess();
        var inherited = new AiTestProcessRequest("/bin/sh", ["-c", "sleep 30 & echo $!"], Path.GetTempPath());
        _grace = Remember(await runner.Run(inherited, TimeSpan.FromSeconds(30), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(3)));
        _deadline = Remember(await runner.Run(inherited, TimeSpan.FromMilliseconds(100), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(3)));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        _cancelled = Remember(await runner.Run(inherited, TimeSpan.FromSeconds(30), cancellation.Token).WaitAsync(TimeSpan.FromSeconds(3)));
        _large = await runner.Run(new("/bin/sh", ["-c", "i=0; while [ $i -lt 20000 ]; do printf 'abcdefghij'; printf '0123456789' >&2; i=$((i+1)); done"], Path.GetTempPath()), TimeSpan.FromSeconds(10), CancellationToken.None);
    }

    [Fact(Skip = "Requires a Unix shell.", SkipUnless = nameof(NonWindows))] void should_bound_drain_after_parent_exit() => _grace.Reason.ShouldContain("Output drain grace exceeded");
    [Fact(Skip = "Requires a Unix shell.", SkipUnless = nameof(NonWindows))] void should_not_treat_abandoned_pipes_as_success() => _grace.Status.ShouldEqual("error");
    [Fact(Skip = "Requires a Unix shell.", SkipUnless = nameof(NonWindows))] void should_apply_deadline_to_pipe_reads() => _deadline.Reason.ShouldContain("Timeout exceeded");
    [Fact(Skip = "Requires a Unix shell.", SkipUnless = nameof(NonWindows))] void should_apply_cancellation_to_pipe_reads() => _cancelled.Reason.ShouldEqual("Cancelled.");
    [Fact(Skip = "Requires a Unix shell.", SkipUnless = nameof(NonWindows))] void should_cap_both_captured_streams() => _large.Output.Length.ShouldEqual(2 * AiTestStreamCapture.Limit);
    [Fact(Skip = "Requires a Unix shell.", SkipUnless = nameof(NonWindows))] void should_capture_stderr_separately() => _large.StandardError.Length.ShouldEqual(AiTestStreamCapture.Limit);
    [Fact(Skip = "Requires a Unix shell.", SkipUnless = nameof(NonWindows))] void should_drain_and_discard_excess_output() => _large.Status.ShouldEqual("completed");

    AiTestProcessResult Remember(AiTestProcessResult result)
    {
        _children.Add(int.Parse(result.Output.Trim(), CultureInfo.InvariantCulture));
        return result;
    }

    void Destroy()
    {
        foreach (var child in _children)
        {
            try
            {
                using var process = Process.GetProcessById(child);
                process.Kill(entireProcessTree: true);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                // A child may already have exited.
            }
        }
    }
}
