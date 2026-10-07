// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_the_stored_secret_is_invalid : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    string? _warning;
    bool _published;

    void Establish() => _server.Stored[_target.Key] = "not-json";

    async Task Because()
    {
        var provider = _server.Provider();
        _warning = await provider.Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None, () => _published = true);
    }

    [Fact] void should_publish_the_replacement() => _published.ShouldBeTrue();
    [Fact] void should_save_the_working_credential() => JsonSerializer.Deserialize<DirectTokens>(_server.Stored[_target.Key])!.RefreshToken.ShouldEqual("new-refresh");
    [Fact] void should_warn_that_the_previous_credential_could_not_be_revoked() => _warning!.ShouldContain("unreadable and could not be revoked");
    [Fact] void should_not_revoke_the_working_replacement() => _server.Revoked.ShouldBeEmpty();

    void Destroy() => _server.Dispose();
}
