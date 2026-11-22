// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesNavigator;

public class when_quitting : Specification
{
    ReleaseNotesNavigator _navigator;
    bool _continueOnQ;
    bool _continueOnEscape;
    bool _continueOnOtherKey;

    void Establish() => _navigator = new([new("3.86.0", "a")]);

    void Because()
    {
        _continueOnQ = _navigator.Handle(ConsoleKey.Q);
        _continueOnEscape = _navigator.Handle(ConsoleKey.Escape);
        _continueOnOtherKey = _navigator.Handle(ConsoleKey.X);
    }

    [Fact] void should_stop_on_q() => _continueOnQ.ShouldBeFalse();
    [Fact] void should_stop_on_escape() => _continueOnEscape.ShouldBeFalse();
    [Fact] void should_continue_on_any_other_key() => _continueOnOtherKey.ShouldBeTrue();
}
