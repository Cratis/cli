// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_removing;

public class with_an_original_empty_collection : given.a_server_registration
{
    const string Before = "{\n\t\"servers\": {},\n\t\"inputs\": []\n}\n";
    string _installed;

    void Establish() => _installed = AiJsonMemberEditor.Set(Before, "servers", "screenplay", _value);
    void Because() => _result = AiJsonMemberEditor.Set(_installed, "servers", "screenplay", null);

    [Fact] void should_restore_every_original_byte() => _result.ShouldEqual(Before);
    [Fact] void should_not_leave_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
}
