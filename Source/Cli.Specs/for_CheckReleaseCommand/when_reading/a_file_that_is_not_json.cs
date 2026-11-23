// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CheckReleaseCommand.when_reading;

public class a_file_that_is_not_json : given.a_release_file
{
    void Because() => Read("{ nope");

    [Fact] void should_be_invalid() => _valid.ShouldBeFalse();
    [Fact] void should_say_it_is_not_valid_json() => _problem.ShouldContain("not valid JSON");
}
