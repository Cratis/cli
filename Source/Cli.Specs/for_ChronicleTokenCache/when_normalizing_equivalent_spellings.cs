// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_normalizing_equivalent_spellings : given.a_token_cache
{
    bool _sameRoot;
    bool _sameIpv6;
    bool _differentIdentity;

    void Because()
    {
        _sameRoot = CachePath(new ChronicleConnectionString("chronicle://client:fixture-secret@localhost:35000/")) == CachePath(Connection());
        _sameIpv6 = CachePath(Connection("[0:0:0:0:0:0:0:1]:35000")) == CachePath(Connection("[::1]:35000"));
        _differentIdentity = CachePath(Connection(client: "client_other"), "production") !=
            CachePath(Connection(client: "other"), "production_client");
    }

    [Fact] void should_ignore_the_root_trailing_slash() => _sameRoot.ShouldBeTrue();
    [Fact] void should_canonicalize_ipv6() => _sameIpv6.ShouldBeTrue();
    [Fact] void should_not_conflate_delimiter_containing_identities() => _differentIdentity.ShouldBeTrue();
}
