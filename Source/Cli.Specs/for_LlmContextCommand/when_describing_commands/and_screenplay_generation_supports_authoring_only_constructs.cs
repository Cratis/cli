// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.LlmContext;

namespace Cratis.Cli.for_LlmContextCommand.when_describing_commands;

public class and_screenplay_generation_supports_authoring_only_constructs : given.a_command_catalog
{
    JsonNode _option;

    void Because() => _option = CommandAt(LlmContextCommand.BuildDescriptorJson(), "screenplay generate")["options"]!
        .AsArray().Single(_ => _!["name"]!.GetValue<string>() == "--authoring-only-constructs")!;

    [Fact] void should_describe_a_boolean_option() => _option["type"]!.GetValue<string>().ShouldEqual("bool");
    [Fact] void should_explain_that_there_is_no_executable_model() => _option["description"]!.GetValue<string>().ShouldContain("no executable model (PLAY0268)");
    [Fact] void should_identify_extraction_and_review_as_the_purpose() => _option["description"]!.GetValue<string>().ShouldContain("extraction and review");
}
