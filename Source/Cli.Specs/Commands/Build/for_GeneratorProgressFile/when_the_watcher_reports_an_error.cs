// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_GeneratorProgressFile;

public class when_the_watcher_reports_an_error : given.a_progress_file
{
    readonly List<string> _steps = [];
    readonly List<string> _diagnostics = [];
    bool _rescannedBeforeCompletion;

    async Task Because()
    {
        var path = Path.Combine(_root, "progress.txt");
        await File.WriteAllTextAsync(path, string.Empty);
        var changes = Changes();
        await using var reader = new GeneratorProgressFile(path, 0, progress => _steps.Add(progress.Step!), _diagnostics.Add, changes);
        await File.AppendAllTextAsync(path, "Recovered after overflow\n");
        await Signal(changes, GeneratorProgressFile.ChangeKind.Error);
        _rescannedBeforeCompletion = _steps.Count == 1;
        await Signal(changes, GeneratorProgressFile.ChangeKind.Error);
        await File.AppendAllTextAsync(path, "A later step\n");
        await Signal(changes);
        await reader.Complete();
    }

    [Fact] void should_rescan_when_a_notification_may_have_been_lost() => _rescannedBeforeCompletion.ShouldBeTrue();
    [Fact] void should_not_replay_previously_read_bytes() => _steps.Count.ShouldEqual(2);
    [Fact] void should_continue_reading_after_the_error() => _steps[^1].ShouldEqual("A later step");
    [Fact] void should_not_invalidate_the_file() => _diagnostics.ShouldBeEmpty();
}
