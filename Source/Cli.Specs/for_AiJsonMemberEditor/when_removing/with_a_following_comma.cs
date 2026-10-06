// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_removing;

public class with_a_following_comma : given.a_server_registration
{
    void Because() => _result = AiJsonMemberEditor.Set("{\n\t\"servers\": {\n\t\t\"screenplay\": {}, \t\n\t\t\"other\": {}\n\t}\n}\n", "servers", "screenplay", null);

    [Fact] void should_remove_the_whole_owned_line_including_the_comma() => _result.ShouldEqual("{\n\t\"servers\": {\n\t\t\"other\": {}\n\t}\n}\n");
    [Fact] void should_not_leave_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
}
