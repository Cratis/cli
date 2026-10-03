// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_the_configuration_cannot_be_saved : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    Exception _error = null!;
    string _previous = null!;

    void Establish()
    {
        _server.Store(_target, "previous-refresh");
        _previous = _server.Stored[_target.Key];
    }

    async Task Because()
    {
        var provider = _server.Provider();
        _error = await Catch.Exception(() => provider.Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None, () => throw new IOException("config cannot be replaced")));
    }

    [Fact] void should_report_the_configuration_failure() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_revoke_only_the_new_token() => _server.Revoked.ShouldContainOnly(["new-refresh"]);
    [Fact] void should_restore_the_entire_previous_credential() => _server.Stored[_target.Key].ShouldEqual(_previous);
    [Fact] void should_retain_the_previous_refresh_token() => JsonSerializer.Deserialize<DirectTokens>(_server.Stored[_target.Key])!.RefreshToken.ShouldEqual("previous-refresh");

    void Destroy() => _server.Dispose();
}
