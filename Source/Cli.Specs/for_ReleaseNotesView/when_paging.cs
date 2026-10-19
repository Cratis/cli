// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;
using Spectre.Console;

namespace Cratis.Cli.for_ReleaseNotesView;

public class when_paging : Specification
{
    StringWriter _output;
    IAnsiConsole _console;
    Queue<ConsoleKey> _keys;

    void Establish()
    {
        _output = new StringWriter();
        _console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(_output),
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Interactive = InteractionSupport.No
        });
        _keys = new([ConsoleKey.RightArrow, ConsoleKey.Q]);
    }

    void Because() => ReleaseNotesView.Page(_console, [new("3.86.0", "Newest notes"), new("3.85.0", "Older notes")], _keys.Dequeue);

    [Fact] void should_end_on_the_release_moved_to() => _output.ToString().ShouldContain("Older notes");
    [Fact] void should_tell_where_the_reader_is() => _output.ToString().ShouldContain("2 of 2");
    [Fact] void should_only_offer_moving_newer_at_the_oldest() => _output.ToString().ShouldContain("\u2190/p newer");
    [Fact] void should_read_every_key() => _keys.ShouldBeEmpty();
}
