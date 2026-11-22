// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Build.for_GeneratorProgressFile;

public class when_reading_a_utf8_preamble : given.a_progress_file
{
    readonly List<string> _steps = [];
    bool _heldPartialPreamble;

    async Task Because()
    {
        var path = Path.Combine(_root, "progress.txt");
        var bytes = Encoding.UTF8.GetBytes("\uFEFFFirst step\n\uFEFFSecond step\n");
        await File.WriteAllBytesAsync(path, bytes[..2]);
        var changes = Changes();
        long offset;
        await using (var reader = new GeneratorProgressFile(path, 0, progress => _steps.Add(progress.Step!), _ => { }, changes))
        {
            await Signal(changes);
            _heldPartialPreamble = _steps.Count == 0;
            await using (var writer = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) await writer.WriteAsync(bytes.AsMemory(2));
            await Signal(changes);
            await reader.Complete();
            offset = reader.Offset;
        }

        var continued = Changes();
        await using var continuation = new GeneratorProgressFile(path, offset, progress => _steps.Add(progress.Step!), _ => { }, continued);
        await File.AppendAllTextAsync(path, "\uFEFFContinuation step\n");
        await Signal(continued);
        await continuation.Complete();
    }

    [Fact] void should_wait_for_all_preamble_bytes() => _heldPartialPreamble.ShouldBeTrue();
    [Fact] void should_strip_the_leading_bom_from_the_first_step() => _steps[0].ShouldEqual("First step");
    [Fact] void should_preserve_the_same_character_in_later_steps() => _steps[1].ShouldEqual("\uFEFFSecond step");
    [Fact] void should_not_strip_characters_after_a_consumed_offset() => _steps[2].ShouldEqual("\uFEFFContinuation step");
}
