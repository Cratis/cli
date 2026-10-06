// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_inserting;

public class with_comments_and_a_trailing_comma : given.a_server_registration
{
    const string Before = "\uFEFF{\r\n\t\"inputs\" : [ /* keep blå */ ],\r\n\t\"servers\" : {\r\n\t\t\"chronicle\" : { \"command\" : \"dotnet\" }, // comma , brace }\r\n\t\t/* user's footer */\r\n";
    const string After = "\t},\r\n\t\"other\" : true,\r\n}\r\n";

    void Because() => _result = AiJsonMemberEditor.Set(Before + After, "servers", "screenplay", _value);

    [Fact] void should_preserve_every_unrelated_byte() => _result.ShouldEqual(Before + Format(Registration, "\t", "\r\n") + "\r\n" + After);
    [Fact] void should_not_create_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
    [Fact] void should_produce_valid_jsonc_with_the_server() => Command().ShouldEqual("cratis");
}
