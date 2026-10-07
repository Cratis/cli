// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_recovery_discovery_is_not_an_object : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    Exception _error = null!;
    string _previous = null!;

    void Establish()
    {
        _server.Store(_target, "previous-refresh");
        _previous = _server.Stored[_target.Key];
        _server.DiscoveryResponse = "[]";
    }

    async Task Because()
    {
        var provider = _server.Provider();
        _error = await Catch.Exception(() => provider.Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None, () => throw new IOException("config cannot be replaced")));
    }

    [Fact] void should_restore_the_entire_previous_credential() => _server.Stored[_target.Key].ShouldEqual(_previous);
    [Fact] void should_report_an_authentication_error() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_report_incomplete_revocation() => _error.Message.ShouldContain("could not be revoked");
    [Fact] void should_not_report_a_successful_revocation() => _server.Revoked.ShouldBeEmpty();

    void Destroy() => _server.Dispose();
}
