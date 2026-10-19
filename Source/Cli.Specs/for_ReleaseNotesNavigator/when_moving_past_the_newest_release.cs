// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesNavigator;

public class when_moving_past_the_newest_release : Specification
{
    ReleaseNotesNavigator _navigator;

    void Establish() => _navigator = new([new("3.86.0", "a"), new("3.85.0", "b"), new("3.84.0", "c")]);

    void Because()
    {
        _navigator.Handle(ConsoleKey.End);
        _navigator.Handle(ConsoleKey.LeftArrow);
        _navigator.Handle(ConsoleKey.P);
        _navigator.Handle(ConsoleKey.PageUp);
    }

    [Fact] void should_stay_on_the_newest() => _navigator.Current.Version.ShouldEqual("3.86.0");
    [Fact] void should_have_nothing_newer() => _navigator.HasNewer.ShouldBeFalse();
    [Fact] void should_have_something_older() => _navigator.HasOlder.ShouldBeTrue();
}
