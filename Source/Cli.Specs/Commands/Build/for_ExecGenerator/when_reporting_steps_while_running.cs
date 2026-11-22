// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_ExecGenerator;

public class when_reporting_steps_while_running : given.a_builder_client
{
    string _root;
    readonly TaskCompletionSource _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _stillRunning;
    bool _succeeded;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), $"cratis-generator-progress-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _studio.OnRequest = path => { if (path.EndsWith("/progress", StringComparison.Ordinal)) _received.TrySetResult(); };
    }

    async Task Because()
    {
        var pipe = Path.Combine(_root, "release");
        var created = await BuildProcess.Run("mkfifo", [pipe], _root);
        created.ExitCode.ShouldEqual(0);
        var order = given.an_order.Parse();
        order.Document["contractVersion"] = "2";
        var work = new StudioBuilderWork("assignment", DateTimeOffset.UtcNow.AddMinutes(1), BuildGitInstructions.Offline, order);
        using var lease = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var reporter = new BuildProgressReporter(_client, work, lease, BuildRedaction.Patterns, _ => { });
        var session = given.a_session.With() with { Project = _root, Workspace = _root, Progress = reporter.Publish };
        await using var generator = new ExecGenerator(session, $"printf '%s\\n' 'Reading the project' >> \"$STUDIO_BUILD_PROGRESS\"; read -r release < '{pipe}'");
        var running = generator.Prompt("Build", new Dictionary<string, string> { [ExecGenerator.ProgressVariable] = Path.Combine(_root, "progress.txt") }, lease.Token);
        await _received.Task.WaitAsync(lease.Token);
        _stillRunning = !running.IsCompleted;
        await using (var writer = new StreamWriter(pipe)) await writer.WriteLineAsync("continue");
        _succeeded = (await running).Succeeded;
        await reporter.Complete();
    }

    void Destroy() => Directory.Delete(_root, true);

    [Fact] void should_reach_studio_before_the_process_exits() => _stillRunning.ShouldBeTrue();
    [Fact] void should_preserve_the_generator_outcome() => _succeeded.ShouldBeTrue();
    [Fact] void should_send_one_step() => _studio.BodiesTo("work/assignment/progress").Count().ShouldEqual(1);
}
