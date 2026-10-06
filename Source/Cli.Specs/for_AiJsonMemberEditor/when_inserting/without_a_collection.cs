// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_inserting;

public class without_a_collection : given.a_server_registration
{
    void Because() => _result = AiJsonMemberEditor.Set("{\n\t\"inputs\": []\n}\n", "servers", "screenplay", _value);

    [Fact] void should_insert_the_collection_and_server_at_their_respective_depths() => _result.ShouldEqual("{\n\t\"inputs\": [],\n\t\"servers\": {\n" + Registration + "\n\t}\n}\n");
    [Fact] void should_not_create_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
    [Fact] void should_produce_valid_json_with_the_server() => Command().ShouldEqual("cratis");
}
