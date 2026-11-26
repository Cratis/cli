// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_refreshing_a_foreign_entry : given.an_oauth_cache
{
    bool _fresh;
    bool _scoped;

    async Task Establish()
    {
        await SeedValidEntry();
        _entry["Binding"] = new JsonObject { ["TokenEndpoint"] = "https://foreign:35000/connect/token", ["ClientId"] = "client" };
        SaveEntry();
    }

    async Task Because()
    {
        await WithProvider(Connection(_endpoint.Server), async provider =>
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            _fresh = _endpoint.IsCurrentToken(await provider.Refresh(deadline.Token));
        });
        var entry = JsonNode.Parse(await File.ReadAllTextAsync(_path));
        _scoped = entry?["ScopeHash"]?.GetValue<string>() is { Length: 64 } &&
            entry["Binding"]?["TokenEndpoint"]?.GetValue<string>() == _endpoint.Endpoint;
    }

    [Fact] void should_reacquire_only_at_the_selected_endpoint() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_return_a_fresh_token() => _fresh.ShouldBeTrue();
    [Fact] void should_write_the_scoped_format() => _scoped.ShouldBeTrue();
    [Fact] void should_use_client_credentials_without_a_refresh_token() => _endpoint.OnlyClientCredentials.ShouldBeTrue();
}
