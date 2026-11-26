// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_an_authority_contains_userinfo : given.a_token_cache
{
    bool _noPath;
    bool _memoryOnly;
    bool _distinct;

    void Because()
    {
        var connection = new ChronicleConnectionString("Host=user@localhost;Port=35000;Username=client;Password=fixture-secret");
        var path = ChronicleTokenCache.GetCachePath(connection, Context);
        _noPath = path is null;
        _distinct = path != CachePath(Connection());
        var provider = ChronicleTokenCache.Create(connection, Context);
        using var lifetime = (IDisposable)provider;
        _memoryOnly = provider is OAuthTokenProvider;
    }

    [Fact] void should_not_assign_a_disk_path() => _noPath.ShouldBeTrue();
    [Fact] void should_not_share_the_plain_hosts_entry() => _distinct.ShouldBeTrue();
    [Fact] void should_use_only_the_in_memory_provider() => _memoryOnly.ShouldBeTrue();
}
