// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_superseding;

public class and_the_credential_cannot_be_removed : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    string? _warning;
    Exception? _error;

    void Establish()
    {
        _server.Store(_target, "previous-refresh");
        _server.DeleteFailure = new UnauthorizedAccessException("denied");
    }

    async Task Because() => _error = await Catch.Exception(async () => _warning = await _server.Provider().Supersede(_target, CancellationToken.None));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_revoke_the_previous_refresh_token() => _server.Revoked.ShouldContainOnly(["previous-refresh"]);
    [Fact] void should_warn_that_it_was_not_removed() => _warning!.ShouldContain("could not be removed");
    [Fact] void should_not_print_the_refresh_token() => _warning!.ShouldNotContain("previous-refresh");

    void Destroy() => _server.Dispose();
}
