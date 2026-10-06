// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_updating;

public class with_four_spaces : given.a_server_registration
{
    const string Before = "{\n\t\"servers\": {\n\t\t\"screenplay\" : ";
    const string After = ",\n\t\t\"other\": {}\n\t},\n\t\"inputs\": []\n}\n";

    void Because() => _result = AiJsonMemberEditor.Set(Format(Before + "{\"command\":\"old\"}" + After, "    "), "servers", "screenplay", _value);

    [Fact] void should_replace_only_the_value_using_four_space_indentation() => _result.ShouldEqual(Format(Before + Registration["\t\t\"screenplay\": ".Length..] + After, "    "));
    [Fact] void should_not_create_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
    [Fact] void should_produce_valid_json_with_the_updated_server() => Command().ShouldEqual("cratis");
}
