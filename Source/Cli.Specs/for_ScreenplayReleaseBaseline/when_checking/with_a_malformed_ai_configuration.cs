// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.when_checking;

public class with_a_malformed_ai_configuration : given.a_release_model
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".cratis"));
        File.WriteAllText(Path.Combine(_root, ".cratis", "ai.json"), "{ not json");
    }

    Task Because() => Run("screenplay", "baseline", "check");
    [Fact] void should_report_that_it_could_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_not_crash() => _error.ShouldNotContain("Unhandled exception");
}
