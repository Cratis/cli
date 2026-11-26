// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_ChronicleTokenCache.given;

public class an_oauth_cache : a_token_cache
{
    protected token_endpoint _endpoint;
    protected string _path;
    protected JsonObject _entry;
    protected bool _receivedFresh;

    async Task Establish()
    {
        _endpoint = new token_endpoint();
        await _endpoint.Start();
        _path = CachePath(Connection(_endpoint.Server));
    }

    protected Task Acquire() => WithProvider(Connection(_endpoint.Server), async provider =>
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token = await provider.GetAccessToken(deadline.Token);
        _receivedFresh = _endpoint.IsCurrentToken(token);
    });

    protected async Task SeedValidEntry()
    {
        await Acquire();
        _entry = JsonNode.Parse(await File.ReadAllTextAsync(_path))!.AsObject();
    }

    protected void SaveEntry() => Seed(_path, _entry.ToJsonString());

    async Task Destroy() => await _endpoint.DisposeAsync();
}
