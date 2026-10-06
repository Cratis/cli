// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiJsonMemberEditor.when_removing;

public class with_crlf : given.a_server_registration
{
    string _installed;

    void Establish() => _installed = AiJsonMemberEditor.Set(Format(Original, "\t", "\r\n"), "servers", "screenplay", _value);
    void Because() => _result = AiJsonMemberEditor.Set(_installed, "servers", "screenplay", null);

    [Fact] void should_restore_the_original_crlf_bytes_exactly() => _result.ShouldEqual(Format(Original, "\t", "\r\n"));
    [Fact] void should_not_leave_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
}
