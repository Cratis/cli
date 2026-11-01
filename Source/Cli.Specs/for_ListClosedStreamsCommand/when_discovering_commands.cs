// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ListClosedStreamsCommand;

public class when_discovering_commands : Specification
{
    CommandNode _closed;

    void Because() => _closed = CliCommandTree.Commands.Single(_ => _.Name == "chronicle").Children.Single(_ => _.Name == "streams").Children.Single(_ => _.Name == "closed");

    [Fact] void should_offer_the_list_and_reopen_commands() => _closed.Children.Select(_ => _.Name).ShouldContainOnly("list", "reopen");
    [Fact] void should_complete_the_scope_dimensions() => _closed.Children.Single(_ => _.Name == "list").Options.ShouldContain("--event-source-id");
}
