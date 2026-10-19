// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesCommand.when_planning_the_presentation;

public class and_output_is_redirected : given.three_releases
{
    void Because() => _result = ReleaseNotesCommand.Plan(OutputFormats.Plain, false, _settings, _releases);

    [Fact] void should_present_as_markdown() => _result.Presentation.ShouldEqual(ReleaseNotesPresentation.Markdown);
    [Fact] void should_present_only_the_latest_release() => _result.Notes.Count.ShouldEqual(1);
}
