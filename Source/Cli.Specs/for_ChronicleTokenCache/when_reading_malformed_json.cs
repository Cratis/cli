// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_reading_malformed_json : given.an_oauth_cache
{
    void Establish() => Seed(_path, "{");

    Task Because() => Acquire();

    [Fact] void should_acquire_a_fresh_token_without_crashing() => _endpoint.Requests.ShouldEqual(1);
    [Fact] void should_return_the_fresh_token() => _receivedFresh.ShouldBeTrue();
}
