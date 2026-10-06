// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiJsonMemberEditor.when_removing;

public class without_an_original_collection : given.a_server_registration
{
    string _installed;

    void Establish() => _installed = AiJsonMemberEditor.Set("{\n\t\"inputs\": []\n}\n", "servers", "screenplay", _value);
    void Because() => _result = AiJsonMemberEditor.Set(_installed, "servers", "screenplay", null);

    [Fact] void should_retain_the_created_collection_as_an_empty_object() => JsonNode.Parse(_result)!["servers"]!.AsObject().Count.ShouldEqual(0);
    [Fact] void should_preserve_the_surrounding_members_and_collection_indentation() => _result.ShouldEqual("{\n\t\"inputs\": [],\n\t\"servers\": {}\n}\n");
    [Fact] void should_not_leave_whitespace_only_lines() => _result.Split('\n').Any(line => line.Length > 0 && string.IsNullOrWhiteSpace(line)).ShouldBeFalse();
    [Fact] void should_not_leave_trailing_whitespace() => HasTrailingWhitespace().ShouldBeFalse();
}
