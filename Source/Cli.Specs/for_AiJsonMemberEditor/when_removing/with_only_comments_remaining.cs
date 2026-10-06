// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_removing;

public class with_only_comments_remaining : given.a_server_registration
{
    void Because() => _result = AiJsonMemberEditor.Set("{\n\t\"servers\": {\n\t\t/* keep */\n\t\t\"screenplay\": {}\n\t}\n}\n", "servers", "screenplay", null);

    [Fact] void should_keep_the_comment_and_surrounding_trivia_without_collapsing_the_object() => _result.ShouldEqual("{\n\t\"servers\": {\n\t\t/* keep */\n\t}\n}\n");
}
