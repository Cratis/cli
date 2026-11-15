// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayGenerationStage;

[Collection(CliSpecsCollection.Name)]
public class when_reporting_progress : Specification
{
    [Theory]
    [InlineData(ScreenplayGenerationStage.Workspace)]
    [InlineData(ScreenplayGenerationStage.Compilation)]
    [InlineData(ScreenplayGenerationStage.Resources)]
    [InlineData(ScreenplayGenerationStage.Generation)]
    public async Task should_report_start_before_delayed_work_completes(string stage)
    {
        var messages = new List<string>();
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = ScreenplayGenerationStage.Run(stage, messages.Add, () => completion.Task, CancellationToken.None);

        messages.ToArray().ShouldEqual([$"{stage}: started"]);
        running.IsCompleted.ShouldBeFalse();
        completion.SetResult(42);
        (await running).ShouldEqual(42);
        messages.ToArray().ShouldEqual([$"{stage}: started", $"{stage}: completed"]);
    }

    [Fact]
    public async Task should_name_the_incomplete_stage_without_reporting_completion_when_cancelled()
    {
        var messages = new List<string>();
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = ScreenplayGenerationStage.Run(ScreenplayGenerationStage.Compilation, messages.Add, () => completion.Task, cancellation.Token);
        await cancellation.CancelAsync();
        completion.SetCanceled(cancellation.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
        messages.ToArray().ShouldEqual(["Compilation/source generators: started", "Compilation/source generators: incomplete"]);
    }

    [Fact]
    public void should_flush_safe_text_and_discard_unknown_payloads()
    {
        var previous = Console.Error;
        using var writer = new flushed_text();
        try
        {
            Console.SetError(writer);
            ScreenplayGenerationStage.Write(OutputFormats.Table, "SECRET_CONFIG_SENTINEL");
            ScreenplayGenerationStage.Write(OutputFormats.Table, $"{ScreenplayGenerationStage.Workspace}: started");
            writer.ToString().ShouldEqual($"{ScreenplayGenerationStage.Workspace}: started{Environment.NewLine}");
            writer.Flushed.ShouldBeTrue();
        }
        finally
        {
            Console.SetError(previous);
        }
    }

    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    [InlineData(OutputFormats.Quiet)]
    public void should_leave_machine_readable_diagnostics_parseable(string format)
    {
        var previous = Console.Error;
        using var writer = new StringWriter();
        try
        {
            Console.SetError(writer);
            ScreenplayGenerationStage.Write(format, $"{ScreenplayGenerationStage.Workspace}: started");
            ScreenplayGenerationStage.Write(format, "SECRET_CONFIG_SENTINEL");
            ScreenplayDiagnosticsWriter.Write(format, [new(ScreenplayDiagnosticSeverity.Warning, "TEST", "safe", null)]);
            using var parsed = JsonDocument.Parse(writer.ToString());
            parsed.RootElement.GetProperty("diagnostics").GetArrayLength().ShouldEqual(1);
        }
        finally
        {
            Console.SetError(previous);
        }
    }

    sealed class flushed_text : StringWriter
    {
        public bool Flushed { get; private set; }
        public override void Flush() => Flushed = true;
    }
}
