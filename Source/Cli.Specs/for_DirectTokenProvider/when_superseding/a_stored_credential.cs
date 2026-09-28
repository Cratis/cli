// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_superseding;

public class a_stored_credential : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    string? _failure;

    void Establish() => _server.Store(_target, "previous-refresh");

    async Task Because() => _failure = await _server.Provider().Supersede(_target, CancellationToken.None);

    [Fact] void should_revoke_the_previous_refresh_token() => _server.Revoked.ShouldContainOnly(["previous-refresh"]);
    [Fact] void should_remove_the_previous_credential() => _server.Stored.ContainsKey(_target.Key).ShouldBeFalse();
    [Fact] void should_report_no_failure() => _failure.ShouldBeNull();

    void Destroy() => _server.Dispose();
}
