// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesCommand.when_planning_the_presentation;

public class and_a_person_is_at_the_terminal_after_an_update : given.three_releases
{
    void Establish()
    {
        _settings.Since = "3.83.0";
    }

    void Because() => _result = ReleaseNotesCommand.Plan(OutputFormats.Table, true, _settings, _releases);

    [Fact] void should_present_as_a_question_then_pager() => _result.Presentation.ShouldEqual(ReleaseNotesPresentation.AskThenPager);
    [Fact] void should_present_every_release() => _result.Notes.Count.ShouldEqual(3);
}
