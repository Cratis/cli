// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectTokenProvider.when_direct_rejects_an_access_token;

public class and_another_process_already_refreshed_it : given.a_stored_unexpired_session
{
    string _access;

    async Task Because() => _access = await _provider.RefreshAccessToken(_target, _issuer, "older-access", CancellationToken.None);

    [Fact] void should_not_refresh_again() => _exchanges.ShouldEqual(0);
    [Fact] void should_return_the_stored_access_token() => _access.ShouldEqual("current-access");
}
