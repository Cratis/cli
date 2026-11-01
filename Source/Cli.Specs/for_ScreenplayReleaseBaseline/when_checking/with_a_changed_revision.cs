// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.when_checking;

public class with_a_changed_revision : given.a_release_model
{
    void Establish()
    {
        ReleaseBaselineCommands.Admit(_root, "v1").ExitCode.ShouldEqual(0);
        File.WriteAllText(_document, Source.Replace("value String", "value Int", StringComparison.Ordinal));
    }
    Task Because() => Run("screenplay", "baseline", "check", _root);
    [Fact] void should_report_violations() => _exitCode.ShouldEqual(1);
    [Fact] void should_name_the_changed_event() => _output.ShouldContain("revision 1");
}
