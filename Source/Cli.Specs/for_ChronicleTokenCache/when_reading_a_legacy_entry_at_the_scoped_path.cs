// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_reading_a_legacy_entry_at_the_scoped_path : given.an_oauth_cache
{
    async Task Establish()
    {
        await SeedValidEntry();
        _entry.Remove("ScopeHash");
        _entry.Remove("Binding");
        SaveEntry();
    }

    Task Because() => Acquire();

    [Fact] void should_acquire_a_fresh_token() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_return_the_fresh_token() => _receivedFresh.ShouldBeTrue();
}
