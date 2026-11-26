// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_recreating_a_provider : given.an_oauth_cache
{
    bool _hasScope;
    bool _hasBinding;

    async Task Establish()
    {
        await SeedValidEntry();
        _hasScope = _entry["ScopeHash"]?.GetValue<string>() is { Length: 64 };
        _hasBinding = _entry["Binding"]?["TokenEndpoint"]?.GetValue<string>() == _endpoint.Endpoint &&
            _entry["Binding"]?["ClientId"]?.GetValue<string>() == "client";
    }

    Task Because() => Acquire();

    [Fact] void should_reuse_the_token_without_another_request() => _endpoint.Requests.ShouldEqual(1);
    [Fact] void should_return_the_matching_token() => _receivedFresh.ShouldBeTrue();
    [Fact] void should_persist_a_complete_scope_hash() => _hasScope.ShouldBeTrue();
    [Fact] void should_persist_the_endpoint_and_client_binding() => _hasBinding.ShouldBeTrue();
}
