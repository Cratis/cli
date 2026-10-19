// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesNavigator;

public class when_moving_past_the_oldest_release : Specification
{
    ReleaseNotesNavigator _navigator;
    bool _continues;

    void Establish() => _navigator = new([new("3.86.0", "a"), new("3.85.0", "b")]);

    void Because()
    {
        _navigator.Handle(ConsoleKey.RightArrow);
        _continues = _navigator.Handle(ConsoleKey.N);
    }

    [Fact] void should_stay_on_the_oldest() => _navigator.Current.Version.ShouldEqual("3.85.0");
    [Fact] void should_report_the_position() => _navigator.Position.ShouldEqual(2);
    [Fact] void should_have_nothing_older() => _navigator.HasOlder.ShouldBeFalse();
    [Fact] void should_have_something_newer() => _navigator.HasNewer.ShouldBeTrue();
    [Fact] void should_continue() => _continues.ShouldBeTrue();
}
