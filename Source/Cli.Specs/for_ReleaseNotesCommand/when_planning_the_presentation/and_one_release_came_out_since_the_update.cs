// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesCommand.when_planning_the_presentation;

public class and_one_release_came_out_since_the_update : given.three_releases
{
    void Establish()
    {
        _settings.Since = "3.85.0";
        _releases = [_releases[0]];
    }

    void Because() => _result = ReleaseNotesCommand.Plan(OutputFormats.Table, true, _settings, _releases);

    [Fact] void should_present_as_formatted_text() => _result.Presentation.ShouldEqual(ReleaseNotesPresentation.Formatted);
    [Fact] void should_present_one_release() => _result.Notes.Count.ShouldEqual(1);
}
