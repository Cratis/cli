// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_removing;

public class with_an_inline_comment : given.a_server_registration
{
    void Because() => _result = AiJsonMemberEditor.Set("{\n\t\"servers\": {\n\t\t\"screenplay\": {}, /* keep */\n\t\t\"other\": {}\n\t}\n}\n", "servers", "screenplay", null);

    [Fact] void should_preserve_the_comment_and_its_surrounding_trivia_exactly() => _result.ShouldEqual("{\n\t\"servers\": {\n\t\t /* keep */\n\t\t\"other\": {}\n\t}\n}\n");
    [Fact] void should_not_leave_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
}
