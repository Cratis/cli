// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Studio.for_StudioFetchCommand;

public class when_fetching : Specification
{
    const string FeatureId = "4a7e2c91-5d3b-4e8f-a1c2-9b8d7e6f5a40";
    JsonObject _arguments;
    string _tool;
    string _content;

    async Task Because()
    {
        var settings = new StudioFetchSettings { Context = "feature", Id = FeatureId, Format = "text", NoGuidance = true };
        _content = await StudioFetchCommand.Fetch(
            (tool, arguments, _) =>
            {
                (_tool, _arguments) = (tool, arguments);
                return Task.FromResult<JsonNode?>(new JsonObject { ["answered"] = true, ["context"] = new JsonObject { ["format"] = "Markdown", ["content"] = "# Invoicing" } });
            },
            settings,
            CancellationToken.None);
    }

    [Fact] void should_use_studios_export() => _tool.ShouldEqual("studio_export_context");
    [Fact] void should_name_the_scope() => _arguments["scope"]!.GetValue<string>().ShouldEqual("feature");
    [Fact] void should_name_the_identifier() => _arguments["id"]!.GetValue<string>().ShouldEqual(FeatureId);
    [Fact] void should_ask_for_markdown_when_text_is_wanted() => _arguments["format"]!.GetValue<string>().ShouldEqual("markdown");
    [Fact] void should_leave_out_the_guidance_when_asked() => _arguments["includeGuidance"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_answer_with_the_content() => _content.ShouldEqual("# Invoicing");
}
