// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Reports only fixed stage labels, never workspace or generator payloads.
/// </summary>
internal sealed class ScreenplayGenerationStage : IDisposable
{
    internal const string Workspace = "Workspace/project loading (including design-time resources)";
    internal const string Resources = "Resource source preparation";
    internal const string Compilation = "Compilation/source generators";
    internal const string Generation = "Screenplay generation";

    static readonly string[] _stages = [Workspace, Resources, Compilation, Generation];
    readonly Action<string> _report;
    readonly string _stage;
    bool _completed;

    internal ScreenplayGenerationStage(string stage, Action<string>? report)
    {
        _stage = stage;
        _report = report ?? (_ => { });
        _report($"{_stage}: started");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_completed)
        {
            _report($"{_stage}: incomplete");
        }
    }

    internal static void Write(string format, string message)
    {
        if (ScreenplayDiagnosticsWriter.IsMachineReadable(format) || !IsKnown(message))
        {
            return;
        }

        Console.Error.WriteLine(message);
        Console.Error.Flush();
    }

    internal static async Task<T> Run<T>(string stage, Action<string>? report, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        using var progress = new ScreenplayGenerationStage(stage, report);
        cancellationToken.ThrowIfCancellationRequested();
        var result = await work();
        cancellationToken.ThrowIfCancellationRequested();
        progress.Complete();
        return result;
    }

    internal void Complete()
    {
        _completed = true;
        _report($"{_stage}: completed");
    }

    static bool IsKnown(string message) => _stages.Any(stage =>
        message == $"{stage}: started" || message == $"{stage}: completed" || message == $"{stage}: incomplete");
}
