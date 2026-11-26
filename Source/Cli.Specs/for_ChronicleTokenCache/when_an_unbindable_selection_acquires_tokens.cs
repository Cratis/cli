// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_an_unbindable_selection_acquires_tokens : given.an_oauth_cache
{
    string _legacy;
    bool _freshSrv;
    bool _freshMultiple;
    bool _noNewFiles;

    async Task Establish()
    {
        await SeedValidEntry();
        _legacy = CliConfiguration.GetTokenCachePath($"{Context}_client");
        Seed(_legacy, _entry.ToJsonString());
        File.Delete(_path);
    }

    async Task Because()
    {
        var srv = new ChronicleConnectionString($"chronicle+srv://client:fixture-secret@{_endpoint.Server}");
        await WithProvider(srv, async provider =>
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            _freshSrv = _endpoint.IsCurrentToken(await provider.GetAccessToken(deadline.Token));
        });
        await WithProvider(Connection($"{_endpoint.Server},other:35000"), async provider =>
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            _freshMultiple = _endpoint.IsCurrentToken(await provider.GetAccessToken(deadline.Token));
        });
        _noNewFiles = !Directory.EnumerateFiles(Path.GetDirectoryName(_legacy)!).Any();
    }

    [Fact] void should_acquire_fresh_tokens_without_reusing_the_persisted_bearer() => _endpoint.Requests.ShouldEqual(3);
    [Fact] void should_return_the_fresh_srv_token() => _freshSrv.ShouldBeTrue();
    [Fact] void should_return_the_fresh_multiple_server_token() => _freshMultiple.ShouldBeTrue();
    [Fact] void should_not_create_any_disk_entries() => _noNewFiles.ShouldBeTrue();
}
