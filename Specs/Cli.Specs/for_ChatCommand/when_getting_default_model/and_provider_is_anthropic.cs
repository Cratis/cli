// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChatCommand.when_getting_default_model;

public class and_provider_is_anthropic : Specification
{
    string _result;

    void Because() => _result = ChatClientFactory.DefaultModel("anthropic");

    [Fact] void should_return_claude_sonnet() => _result.ShouldEqual("claude-sonnet-4-20250514");
}
