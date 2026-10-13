// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.when_checking;

public class with_an_unknown_option : given.a_release_model
{
    Task Because() => Run("screenplay", "baseline", "check", _root, "--typo");
    [Fact] void should_reject_usage() => _exitCode.ShouldEqual(255);
    [Fact] void should_name_the_unknown_option() => _error.ShouldContain("Unknown option");
}
