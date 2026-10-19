// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_CliUpdate;

public class when_creating_the_release_notes_process : Specification
{
    ProcessStartInfo _result;

    void Because() => _result = CliUpdate.CreateReleaseNotesProcessStartInfo("3.85.0", OutputFormats.Plain, true);

    [Fact] void should_run_the_installed_version_from_the_path() => _result.FileName.ShouldEqual("cratis");
    [Fact] void should_ask_for_the_releases_since_the_previous_version_in_the_same_format() => _result.ArgumentList.ToArray().ShouldEqual(["release-notes", "--since", "3.85.0", "--output", "plain", "--yes"]);
    [Fact] void should_share_this_console() => _result.RedirectStandardOutput.ShouldBeFalse();
}
