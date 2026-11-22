// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_GeneratorProgressFile;

public class when_a_generator_does_not_append : given.a_progress_file
{
    readonly List<BuildProgress> _steps = [];
    readonly List<string> _diagnostics = [];

    async Task Because()
    {
        var path = Path.Combine(_root, "progress.txt");
        await File.WriteAllTextAsync(path, "An earlier step\n");
        await using (var empty = new GeneratorProgressFile(path, new FileInfo(path).Length, _steps.Add, _diagnostics.Add, Changes())) await empty.Complete();
        var changes = Changes();
        await using (var truncated = new GeneratorProgressFile(path, new FileInfo(path).Length, _steps.Add, _diagnostics.Add, changes))
        {
            await File.WriteAllTextAsync(path, string.Empty);
            await Signal(changes);
            await File.AppendAllTextAsync(path, "Ignore after truncation\n");
            await Signal(changes);
            await truncated.Complete();
        }

        var replacement = Changes();
        await using var replaced = new GeneratorProgressFile(path, 0, _steps.Add, _diagnostics.Add, replacement);
        File.Move(path, Path.Combine(_root, "old.txt"));
        await Signal(replacement);
        await File.WriteAllTextAsync(path, "Do not replay\n");
        await Signal(replacement);
        await replaced.Complete();
    }

    [Fact] void should_not_invent_or_replay_steps() => _steps.ShouldBeEmpty();
    [Fact] void should_diagnose_truncation_and_replacement_once_each() => _diagnostics.Count.ShouldEqual(2);
}
