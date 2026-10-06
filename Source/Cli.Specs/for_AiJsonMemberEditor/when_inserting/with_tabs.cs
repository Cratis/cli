// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_inserting;

public class with_tabs : given.a_server_registration
{
    void Because() => _result = AiJsonMemberEditor.Set(Original, "servers", "screenplay", _value);

    [Fact] void should_match_the_existing_indentation_and_nesting_exactly() => _result.ShouldEqual(Expected);
    [Fact] void should_not_create_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
    [Fact] void should_produce_valid_json_with_the_server() => Command().ShouldEqual("cratis");
}
