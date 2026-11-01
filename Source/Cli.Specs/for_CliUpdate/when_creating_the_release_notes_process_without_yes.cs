// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_CliUpdate;

public class when_creating_the_release_notes_process_without_yes : Specification
{
    ProcessStartInfo _result;

    void Because() => _result = CliUpdate.CreateReleaseNotesProcessStartInfo("3.85.0", OutputFormats.Table, false);

    [Fact] void should_leave_the_question_to_the_installed_version() => _result.ArgumentList.ToArray().ShouldEqual(["release-notes", "--since", "3.85.0", "--output", "table"]);
}
