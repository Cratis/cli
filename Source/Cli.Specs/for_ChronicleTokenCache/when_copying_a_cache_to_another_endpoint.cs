// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_copying_a_cache_to_another_endpoint : given.an_oauth_cache
{
    given.token_endpoint _other;

    async Task Establish()
    {
        await SeedValidEntry();
        _other = new given.token_endpoint();
        await _other.Start();
        Seed(CachePath(Connection(_other.Server)), _entry.ToJsonString());
    }

    Task Because() => WithProvider(Connection(_other.Server), async provider =>
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        _receivedFresh = _other.IsCurrentToken(await provider.GetAccessToken(deadline.Token));
    });

    async Task Destroy() => await _other.DisposeAsync();

    [Fact] void should_acquire_from_the_selected_endpoint() => _other.Requests.ShouldEqual(1);
    [Fact] void should_not_contact_the_original_endpoint_again() => _endpoint.Requests.ShouldEqual(1);
    [Fact] void should_return_the_selected_endpoints_token() => _receivedFresh.ShouldBeTrue();
}
