// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_a_host_has_a_trailing_dot : given.a_token_cache
{
    bool _notShared;

    void Because()
    {
        var path = ChronicleTokenCache.GetCachePath(Connection("localhost.:35000"), Context);
        _notShared = path is null || path != CachePath(Connection());
    }

    [Fact] void should_not_share_the_plain_hosts_disk_entry() => _notShared.ShouldBeTrue();
}
