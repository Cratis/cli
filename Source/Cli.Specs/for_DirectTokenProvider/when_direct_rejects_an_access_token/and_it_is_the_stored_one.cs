// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectTokenProvider.when_direct_rejects_an_access_token;

public class and_it_is_the_stored_one : given.a_stored_unexpired_session
{
    string _access;

    async Task Because()
    {
        _access = await _provider.RefreshAccessToken(_target, _issuer, "current-access", CancellationToken.None);
        _refresh = await StoredRefreshToken();
    }

    [Fact] void should_refresh_although_it_has_not_expired() => _exchanges.ShouldEqual(1);
    [Fact] void should_return_the_new_access_token() => _access.ShouldEqual("new-access");
    [Fact] void should_save_the_rotated_refresh_token() => _refresh.ShouldEqual("rotated-refresh");
}
