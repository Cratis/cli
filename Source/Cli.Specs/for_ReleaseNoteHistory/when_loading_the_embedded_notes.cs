// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNoteHistory;

public class when_loading_the_embedded_notes : Specification
{
    ReleaseNoteHistory _history;

    void Because() => _history = ReleaseNoteHistory.Embedded();

    [Fact] void should_carry_the_first_release() => _history.Find("1.0.0")!.Notes.ShouldNotBeEmpty();
    [Fact] void should_carry_many_releases() => (_history.Notes.Count > 100).ShouldBeTrue();
    [Fact] void should_give_every_release_notes() => _history.Notes.Where(_ => string.IsNullOrWhiteSpace(_.Notes)).ShouldBeEmpty();
}
