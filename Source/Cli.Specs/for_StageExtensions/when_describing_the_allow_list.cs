// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Run.for_StageExtensions;

public class when_describing_the_allow_list : given.a_plan
{
    JsonNode _restricted;
    JsonNode _open;

    void Establish() => Create(references: ["ghcr.io/cratis/*"]);

    void Because()
    {
        _restricted = JsonNode.Parse(Plan.AllowListJson)!;
        Create();
        _open = JsonNode.Parse(Plan.AllowListJson)!;
    }

    [Fact] void should_allow_the_publisher() => _restricted["allow"]!["publishers"]![0]!.GetValue<string>().ShouldEqual(Publisher);
    [Fact] void should_limit_references_when_every_publisher_is_limited() => _restricted["allow"]!["references"]![0]!.GetValue<string>().ShouldEqual("ghcr.io/cratis/*");
    [Fact] void should_not_limit_references_when_a_publisher_is_not() => _open["allow"]!["references"].ShouldBeNull();
    [Fact] void should_give_stage_the_key_to_verify_the_publisher_with() => _restricted["publishers"]![Publisher]!["keys"]![0]!.GetValue<string>().ShouldEqual(PublisherKey);
}
