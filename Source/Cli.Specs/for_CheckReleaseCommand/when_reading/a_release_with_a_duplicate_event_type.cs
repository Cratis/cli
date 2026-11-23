// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CheckReleaseCommand.when_reading;

public class a_release_with_a_duplicate_event_type : given.a_release_file
{
    void Because()
    {
        const string item = "{ \"type\": { \"id\": \"BookAdded\", \"generation\": 1 }, \"schema\": \"{}\" }";
        Read(Valid.Replace(item, item + ", " + item));
    }

    [Fact] void should_be_invalid() => _valid.ShouldBeFalse();
    [Fact] void should_name_the_duplicate() => _problem.ShouldContain("'BookAdded' is listed more than once");
}
