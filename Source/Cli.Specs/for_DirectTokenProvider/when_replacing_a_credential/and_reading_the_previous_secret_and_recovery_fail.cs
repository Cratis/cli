// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_reading_the_previous_secret_and_recovery_fail : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    Exception _error = null!;

    void Establish()
    {
        _server.Store(_target, "previous-refresh");
        _server.ReadFailure = new DirectAuthError("Credential manager is locked.");
        _server.RefusedTokens.Add("new-refresh");
    }

    async Task Because()
    {
        var provider = _server.Provider();
        _error = await Catch.Exception(() => provider.Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None));
    }

    [Fact] void should_report_incomplete_recovery() => _error.Message.ShouldContain("The new Direct refresh token could not be revoked");
    [Fact] void should_report_a_safe_authentication_error() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_not_discard_a_temporarily_inaccessible_credential() => _server.Stored[_target.Key].ShouldContain("previous-refresh");
    [Fact] void should_not_disclose_the_new_token() => _error.Message.ShouldNotContain("new-refresh");

    void Destroy() => _server.Dispose();
}
