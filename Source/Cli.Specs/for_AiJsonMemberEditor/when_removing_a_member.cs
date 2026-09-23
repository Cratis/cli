// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiJsonMemberEditor;

public class when_removing_a_member : Specification
{
    string[] _inputs;
    string[] _results;

    void Establish() => _inputs =
    [
        """{"servers":{"screenplay":{},"other":{}}}""",
        """{"servers":{"other":{},"screenplay":{}}}""",
        """{"servers":{"one":{},"screenplay":{},"other":{}}}""",
        """{"servers":{"screenplay":{}}}""",
        """{"servers":{"screenplay":{},}}""",
        """{"servers":{"other":{} /* , comment */,"screenplay":{} /* , keep */}}""",
        """{"servers":{"screenplay":{} /* , keep */,"other":{}}}"""
    ];

    void Because() => _results = [.. _inputs.Select(value => AiJsonMemberEditor.Set(value, "servers", "screenplay", null))];

    [Fact] void should_exercise_seven_separator_shapes() => _results.Length.ShouldEqual(7);
    [Fact] void should_leave_valid_jsonc_without_the_owned_member() => _results.All(value => !JsonNode.Parse(value, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip })!["servers"]!.AsObject().ContainsKey("screenplay")).ShouldBeTrue();
    [Fact] void should_keep_comments_after_the_removed_value() => _results[^1].Contains("/* , keep */", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_other_server_bytes() => _results.Where((_, index) => index is not 3 and not 4).All(value => value.Contains("\"other\":{}", StringComparison.Ordinal)).ShouldBeTrue();
}
