// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesNavigator;

public class when_quitting : Specification
{
    ReleaseNotesNavigator _navigator;

    void Establish() => _navigator = new([new("3.86.0", "a")]);

    [Fact] void should_stop_on_q() => _navigator.Handle(ConsoleKey.Q).ShouldBeFalse();
    [Fact] void should_stop_on_escape() => _navigator.Handle(ConsoleKey.Escape).ShouldBeFalse();
    [Fact] void should_continue_on_any_other_key() => _navigator.Handle(ConsoleKey.X).ShouldBeTrue();
}
