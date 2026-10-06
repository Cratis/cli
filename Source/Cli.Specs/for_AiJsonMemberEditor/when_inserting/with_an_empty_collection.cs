// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_inserting;

public class with_an_empty_collection : given.a_server_registration
{
    void Because() => _result = AiJsonMemberEditor.Set("{\n\t\"servers\": {},\n\t\"inputs\": []\n}\n", "servers", "screenplay", _value);

    [Fact] void should_expand_the_object_at_its_existing_depth() => _result.ShouldEqual("{\n\t\"servers\": {\n" + Registration + "\n\t},\n\t\"inputs\": []\n}\n");
    [Fact] void should_not_create_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
    [Fact] void should_produce_valid_json_with_the_server() => Command().ShouldEqual("cratis");
}
