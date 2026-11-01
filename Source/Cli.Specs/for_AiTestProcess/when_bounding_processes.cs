// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestProcess;

public class when_bounding_processes : Specification
{
    AiTestProcessResult _timeout = null!;
    AiTestProcessResult _cancelled = null!;
    AiTestProcessResult _missing = null!;
    async Task Because()
    {
        var runner = new AiTestProcess();
        _timeout = await runner.Run(new("/bin/sh", ["-c", "sleep 10 & wait"], Path.GetTempPath()), TimeSpan.FromMilliseconds(100), CancellationToken.None);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        _cancelled = await runner.Run(new("/bin/sh", ["-c", "sleep 10 & wait"], Path.GetTempPath()), TimeSpan.FromSeconds(10), cancellation.Token);
        _missing = await runner.Run(new("cratis-spec-nonexistent-executable", [], Path.GetTempPath()), TimeSpan.FromSeconds(1), CancellationToken.None);
    }
    [Fact] void should_report_timeout_as_error() => _timeout.Status.ShouldEqual("error");
    [Fact] void should_explain_timeout() => _timeout.Reason.ShouldContain("Timeout exceeded");
    [Fact] void should_report_cancellation_as_error() => _cancelled.Status.ShouldEqual("error");
    [Fact] void should_explain_cancellation() => _cancelled.Reason.ShouldEqual("Cancelled.");
    [Fact] void should_skip_missing_executable() => _missing.Status.ShouldEqual("skipped");
}
