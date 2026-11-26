// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_the_authority_is_not_unique : given.a_token_cache
{
    string _srvPath;
    string _multiplePath;
    bool _srvIsMemoryOnly;
    bool _multipleIsMemoryOnly;

    void Because()
    {
        var srv = new ChronicleConnectionString("chronicle+srv://client:fixture-secret@localhost");
        var multiple = Connection("localhost:35000,other:35000");
        _srvPath = ChronicleTokenCache.GetCachePath(srv, Context);
        _multiplePath = ChronicleTokenCache.GetCachePath(multiple, Context);
        var srvProvider = ChronicleTokenCache.Create(srv, Context);
        var multipleProvider = ChronicleTokenCache.Create(multiple, Context);
        using var srvLifetime = (IDisposable)srvProvider;
        using var multipleLifetime = (IDisposable)multipleProvider;
        _srvIsMemoryOnly = srvProvider is OAuthTokenProvider;
        _multipleIsMemoryOnly = multipleProvider is OAuthTokenProvider;
        ChronicleTokenCache.Clear(srv, Context);
        ChronicleTokenCache.Clear(multiple, Context);
    }

    [Fact] void should_not_assign_a_disk_path_to_srv() => (_srvPath is null).ShouldBeTrue();
    [Fact] void should_not_assign_a_disk_path_to_multiple_servers() => (_multiplePath is null).ShouldBeTrue();
    [Fact] void should_use_an_in_memory_provider_for_srv() => _srvIsMemoryOnly.ShouldBeTrue();
    [Fact] void should_use_an_in_memory_provider_for_multiple_servers() => _multipleIsMemoryOnly.ShouldBeTrue();
}
