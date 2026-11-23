// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CheckReleaseCommand.when_reading;

public class a_release_without_a_client : given.a_release_file
{
    void Because() => Read(Valid.Replace("\"client\"", "\"other\""));

    [Fact] void should_be_invalid() => _valid.ShouldBeFalse();
    [Fact] void should_say_the_client_is_required() => _problem.ShouldContain("'client' is required");
}
