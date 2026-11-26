// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_selecting_a_different_transport : given.a_token_cache
{
    bool _different;

    void Because() => _different = CachePath(Connection()) != CachePath(Connection(disableTls: true));

    [Fact] void should_partition_the_disk_cache() => _different.ShouldBeTrue();
}
