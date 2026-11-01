// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesCommand.when_planning_the_presentation;

public class and_nobody_can_press_keys : given.three_releases
{
    void Because() => _result = ReleaseNotesCommand.Plan(OutputFormats.Table, false, _settings, _releases);

    [Fact] void should_present_as_formatted_text() => _result.Presentation.ShouldEqual(ReleaseNotesPresentation.Formatted);
    [Fact] void should_present_only_the_latest_release() => _result.Notes.Count.ShouldEqual(1);
}
