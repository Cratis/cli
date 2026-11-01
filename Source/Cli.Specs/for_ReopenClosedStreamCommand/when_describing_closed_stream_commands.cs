// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.LlmContext;

namespace Cratis.Cli.for_ReopenClosedStreamCommand;

public class when_describing_closed_stream_commands : for_LlmContextCommand.given.a_command_catalog
{
    JsonObject _list;
    JsonObject _reopen;

    void Because()
    {
        var catalog = LlmContextCommand.BuildDescriptorJson();
        _list = CommandAt(catalog, "chronicle streams closed list");
        _reopen = CommandAt(catalog, "chronicle streams closed reopen");
    }

    [Fact] void should_describe_listing_as_read_only() => _list["effect"]!.GetValue<string>().ShouldEqual("read-only");
    [Fact] void should_not_confirm_listing() => _list["requiresConfirmation"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_describe_reopening_as_mutating() => _reopen["effect"]!.GetValue<string>().ShouldEqual("mutating");
    [Fact] void should_confirm_reopening() => _reopen["requiresConfirmation"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_document_the_authenticated_actor_requirement() => _reopen["description"]!.GetValue<string>().ShouldContain("authenticated principal with a subject");
    [Fact] void should_document_the_minimum_version() => _reopen["description"]!.GetValue<string>().ShouldContain("19.39.0");
    [Fact] void should_describe_the_repair_reason() => _reopen["options"]!.AsArray().Select(_ => _!["name"]!.GetValue<string>()).ShouldContain("--reason");
}
