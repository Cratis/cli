// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_RunCommand.given;

namespace Cratis.Cli.for_StageSession;

[Collection(CliSpecsCollection.Name)]
public class when_waiting_for_runtime_admission : a_serving_stage
{
    [Theory]
    [InlineData("ready", true)]
    [InlineData("unsupported", false)]
    async Task should_wait_for_a_terminal_runtime_state(string state, bool ready)
    {
        if (OperatingSystem.IsWindows()) return;
        _statusBody = $"{{\"state\":\"{state}\",\"engine\":\"semantic\"}}";
        using var session = Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            (await session.WaitUntilReady(_port, () => { }, deadline.Token)).ShouldEqual(ready);
            session.RuntimeStatus!.Engine.ShouldEqual("semantic");
            session.Startup.Phase.ShouldNotEqual(StagePhase.Running);
        }
        finally
        {
            await session.Stop();
            await session.WaitForExit(deadline.Token);
        }
    }

    [Theory]
    [InlineData("eventmodel")]
    [InlineData(null)]
    async Task should_not_report_ready_before_registration_for_non_semantic_engines(string? engine)
    {
        if (OperatingSystem.IsWindows()) return;
        _statusBody = engine is null ? "{\"state\":\"ready\"}" : $"{{\"state\":\"ready\",\"engine\":\"{engine}\"}}";
        using var session = Start();
        using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            var error = await Catch.Exception(() => session.WaitUntilReady(_port, cancel.Cancel, cancel.Token));
            (error is OperationCanceledException).ShouldBeTrue();
            session.RuntimeStatus!.State.ShouldEqual("ready");
            session.Startup.Phase.ShouldNotEqual(StagePhase.Running);
        }
        finally
        {
            await session.Stop();
            await session.WaitForExit(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData("eventmodel")]
    [InlineData(null)]
    async Task should_report_ready_after_registration_for_non_semantic_engines(string? engine)
    {
        if (OperatingSystem.IsWindows()) return;
        _statusBody = engine is null ? "{\"state\":\"ready\"}" : $"{{\"state\":\"ready\",\"engine\":\"{engine}\"}}";
        using var session = Start();
        session.Startup.Observe("has no read models with projections");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            (await session.WaitUntilReady(_port, () => { }, deadline.Token)).ShouldBeTrue();
            session.RuntimeStatus!.State.ShouldEqual("ready");
        }
        finally
        {
            await session.Stop();
            await session.WaitForExit(deadline.Token);
        }
    }

    [Theory]
    [InlineData("warm")]
    [InlineData("loading")]
    async Task should_not_report_readiness_from_registration_logs_while_admission_is_pending(string state)
    {
        if (OperatingSystem.IsWindows()) return;
        _statusBody = $"{{\"state\":\"{state}\"}}";
        using var session = Start();
        session.Startup.Observe("has no read models with projections");
        using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            var error = await Catch.Exception(() => session.WaitUntilReady(_port, cancel.Cancel, cancel.Token));
            (error is OperationCanceledException).ShouldBeTrue();
            session.RuntimeStatus!.IsPending.ShouldBeTrue();
        }
        finally
        {
            await session.Stop();
            await session.WaitForExit(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(404, "not found")]
    [InlineData(200, "<html>legacy</html>")]
    async Task should_preserve_registration_readiness_for_older_images(int code, string body)
    {
        if (OperatingSystem.IsWindows()) return;
        _statusCode = code;
        _statusBody = body;
        using var session = Start();
        session.Startup.Observe("has no read models with projections");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            (await session.WaitUntilReady(_port, () => { }, deadline.Token)).ShouldBeTrue();
            session.RuntimeStatus.ShouldBeNull();
        }
        finally
        {
            await session.Stop();
            await session.WaitForExit(deadline.Token);
        }
    }

    StageSession Start() => StageSession.Start("sandbox", "image", StageContainer.BuildRunArguments(_folder, "latest", _port, 35000, "sandbox"), captureOutput: true)!;
}
