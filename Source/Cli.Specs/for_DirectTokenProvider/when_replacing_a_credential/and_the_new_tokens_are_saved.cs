// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_the_new_tokens_are_saved : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    string? _warning;
    bool _published;
    bool _publishedBeforeRevocation;
    string? _storedBeforeRevocation;

    void Establish()
    {
        _server.Store(_target, "previous-refresh");
        _server.BeforeRevocation = _ =>
        {
            _publishedBeforeRevocation = _published;
            _storedBeforeRevocation = JsonSerializer.Deserialize<DirectTokens>(_server.Stored[_target.Key])!.RefreshToken;
        };
    }

    async Task Because()
    {
        var provider = _server.Provider();
        _warning = await provider.Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None, () => _published = true);
    }

    [Fact] void should_revoke_only_the_previous_refresh_token() => _server.Revoked.ShouldContainOnly(["previous-refresh"]);
    [Fact] void should_store_the_new_refresh_token() => JsonSerializer.Deserialize<DirectTokens>(_server.Stored[_target.Key])!.RefreshToken.ShouldEqual("new-refresh");
    [Fact] void should_report_no_warning() => _warning.ShouldBeNull();
    [Fact] void should_publish_before_revoking_the_previous_token() => _publishedBeforeRevocation.ShouldBeTrue();
    [Fact] void should_save_before_revoking_the_previous_token() => _storedBeforeRevocation.ShouldEqual("new-refresh");

    void Destroy() => _server.Dispose();
}
