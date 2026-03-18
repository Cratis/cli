// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChatCommand.when_resolving_api_key;

public class and_key_is_literal : Specification
{
    string _result;

    void Because() => _result = ChatClientFactory.ResolveApiKey("sk-direct-key");

    [Fact] void should_return_key_as_is() => _result.ShouldEqual("sk-direct-key");
}
