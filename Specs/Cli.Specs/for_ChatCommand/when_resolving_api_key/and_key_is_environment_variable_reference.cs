// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChatCommand.when_resolving_api_key;

[Collection(CliSpecsCollection.Name)]
public class and_key_is_environment_variable_reference : Specification
{
    string? _previousValue;
    string _result;

    void Establish()
    {
        _previousValue = Environment.GetEnvironmentVariable("TEST_CRATIS_API_KEY");
        Environment.SetEnvironmentVariable("TEST_CRATIS_API_KEY", "sk-test-12345");
    }

    void Because() => _result = ChatClientFactory.ResolveApiKey("$TEST_CRATIS_API_KEY");

    [Fact] void should_resolve_from_environment() => _result.ShouldEqual("sk-test-12345");

    void Destroy() => Environment.SetEnvironmentVariable("TEST_CRATIS_API_KEY", _previousValue);
}
