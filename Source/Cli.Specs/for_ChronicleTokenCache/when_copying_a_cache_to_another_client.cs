// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_copying_a_cache_to_another_client : given.an_oauth_cache
{
    async Task Establish()
    {
        await SeedValidEntry();
        Seed(CachePath(Connection(_endpoint.Server, client: "other")), _entry.ToJsonString());
    }

    Task Because() => WithProvider(Connection(_endpoint.Server, client: "other"), async provider =>
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        _receivedFresh = _endpoint.IsCurrentToken(await provider.GetAccessToken(deadline.Token));
    });

    [Fact] void should_acquire_a_fresh_token_for_the_selected_client() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_return_the_fresh_token() => _receivedFresh.ShouldBeTrue();
}
