// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_updating;

public class with_an_existing_entry : given.a_server_registration
{
    const string Before = "{\r\n\t\"servers\": {\r\n\t\t/* keep */ \"chronicle\": {},\r\n\t\t\"screenplay\" : ";
    const string After = ", /* user's comment */\r\n\t\t\"other\": {}\r\n\t},\r\n\t\"inputs\": []\r\n}\r\n";

    void Because() => _result = AiJsonMemberEditor.Set(Before + "{\"command\":\"old\"}" + After, "servers", "screenplay", _value);

    [Fact] void should_replace_only_the_value_using_existing_indentation() => _result.ShouldEqual(Before + Format(Registration["\t\t\"screenplay\": ".Length..], "\t", "\r\n") + After);
    [Fact] void should_not_create_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
    [Fact] void should_produce_valid_json_with_the_updated_server() => Command().ShouldEqual("cratis");
}
