// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text;

namespace Cratis.Cli.Commands.Build.for_GeneratorProgressFile;

public class when_reading_appended_steps : given.a_progress_file
{
    readonly ConcurrentQueue<string> _steps = new();
    bool _heldPartialCharacter;
    bool _readBeforeCompletion;
    long _offset;

    async Task Because()
    {
        var path = Path.Combine(_root, "progress.txt");
        await File.WriteAllTextAsync(path, string.Empty);
        var changes = Changes();
        await using (var reader = new GeneratorProgressFile(path, 0, progress => _steps.Enqueue(progress.Step!), _ => { }, changes))
        {
            await using (var writer = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                var unicode = Encoding.UTF8.GetBytes("Réading");
                await writer.WriteAsync(unicode.AsMemory(0, 2));
                await writer.FlushAsync();
                await Signal(changes);
                _heldPartialCharacter = _steps.IsEmpty;
                await writer.WriteAsync(unicode.AsMemory(2));
                await writer.WriteAsync(Encoding.UTF8.GetBytes("\r\n\n"));
                await writer.FlushAsync();
                await Signal(changes);
                _readBeforeCompletion = _steps.Count == 1;
                await writer.WriteAsync(Encoding.UTF8.GetBytes("Final step"));
            }

            await reader.Complete();
            _offset = reader.Offset;
        }

        var continued = Changes();
        await using var continuation = new GeneratorProgressFile(path, _offset, progress => _steps.Enqueue(progress.Step!), _ => { }, continued);
        await File.AppendAllTextAsync(path, "\nContinued\n");
        await Signal(continued);
        await continuation.Complete();
    }

    [Fact] void should_hold_a_split_utf8_character_until_its_remaining_bytes_arrive() => _heldPartialCharacter.ShouldBeTrue();
    [Fact] void should_observe_unicode_before_process_completion() => _readBeforeCompletion.ShouldBeTrue();
    [Fact] void should_decode_unicode_and_crlf() => _steps.First().ShouldEqual("Réading");
    [Fact] void should_drain_a_final_unterminated_line() => _steps.ElementAt(1).ShouldEqual("Final step");
    [Fact] void should_not_replay_earlier_bytes_on_continuation() => _steps.Count.ShouldEqual(3);
    [Fact] void should_ignore_blank_lines() => _steps.Last().ShouldEqual("Continued");
}
