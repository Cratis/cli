// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesCommand.when_planning_the_presentation;

public class and_json_is_asked_for : given.three_releases
{
    void Because() => _result = ReleaseNotesCommand.Plan(OutputFormats.Json, false, _settings, _releases);

    [Fact] void should_present_as_json() => _result.Presentation.ShouldEqual(ReleaseNotesPresentation.Json);
    [Fact] void should_present_every_release() => _result.Notes.Count.ShouldEqual(3);
}
