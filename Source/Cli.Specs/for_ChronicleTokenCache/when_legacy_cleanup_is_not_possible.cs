// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_legacy_cleanup_is_not_possible : given.an_oauth_cache
{
    string _legacy;

    void Establish()
    {
        _legacy = CliConfiguration.GetTokenCachePath($"{Context}_client");
        Directory.CreateDirectory(_legacy);
    }

    Task Because() => Acquire();

    [Fact] void should_acquire_a_fresh_token_despite_the_cleanup_error() => _endpoint.Requests.ShouldEqual(1);
    [Fact] void should_return_the_fresh_token() => _receivedFresh.ShouldBeTrue();
    [Fact] void should_not_remove_an_unexpected_directory() => Directory.Exists(_legacy).ShouldBeTrue();
    [Fact] void should_still_persist_the_scoped_token() => File.Exists(_path).ShouldBeTrue();
}
