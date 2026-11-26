// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_copying_a_cache_to_another_transport : given.an_oauth_cache
{
    async Task Establish()
    {
        await SeedValidEntry();
        Seed(CachePath(Connection(_endpoint.Server, disableTls: true)), _entry.ToJsonString());
    }

    Task Because() => WithProvider(Connection(_endpoint.Server, disableTls: true), async provider =>
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        _receivedFresh = _endpoint.IsCurrentToken(await provider.GetAccessToken(deadline.Token));
    });

    [Fact] void should_acquire_at_the_same_https_endpoint_instead_of_reusing_the_foreign_scope() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_return_the_fresh_token() => _receivedFresh.ShouldBeTrue();
}
