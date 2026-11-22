// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_GeneratorProgressFile;

public class when_a_line_exceeds_the_limit : given.a_progress_file
{
    readonly List<string> _steps = [];
    readonly List<string> _diagnostics = [];

    async Task Because()
    {
        var path = Path.Combine(_root, "progress.txt");
        await File.WriteAllTextAsync(path, string.Empty);
        var changes = Changes();
        await using var reader = new GeneratorProgressFile(path, 0, progress => _steps.Add(progress.Step!), _diagnostics.Add, changes);
        await File.AppendAllTextAsync(path, new string('x', 65537));
        await Signal(changes);
        await File.AppendAllTextAsync(path, "\nA bounded step\n");
        await Signal(changes);
        await reader.Complete();
    }

    [Fact] void should_drop_the_oversized_line() => _steps.Count.ShouldEqual(1);
    [Fact] void should_continue_with_the_next_line() => _steps.Single().ShouldEqual("A bounded step");
    [Fact] void should_report_the_dropped_line_once() => _diagnostics.Count.ShouldEqual(1);
}
