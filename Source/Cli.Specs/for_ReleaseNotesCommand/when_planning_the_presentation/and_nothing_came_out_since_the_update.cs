// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesCommand.when_planning_the_presentation;

public class and_nothing_came_out_since_the_update : given.three_releases
{
    void Establish() => _settings.Since = "3.86.0";

    void Because() => _result = ReleaseNotesCommand.Plan(OutputFormats.Table, true, _settings, []);

    [Fact] void should_show_nothing() => _result.Presentation.ShouldEqual(ReleaseNotesPresentation.Nothing);
}
