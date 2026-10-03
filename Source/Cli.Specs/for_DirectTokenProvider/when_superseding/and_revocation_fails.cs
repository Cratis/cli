// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_superseding;

public class and_revocation_fails : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    string? _failure;

    void Establish()
    {
        _server.Store(_target, "previous-refresh");
        _server.RefusedTokens.Add("previous-refresh");
    }

    async Task Because() => _failure = await _server.Provider().Supersede(_target, CancellationToken.None);

    [Fact] void should_report_the_failure() => _failure!.ShouldContain("HTTP 500");
    [Fact] void should_not_print_the_refresh_token() => _failure!.ShouldNotContain("previous-refresh");
    [Fact] void should_still_remove_the_replaced_credential() => _server.Stored.ContainsKey(_target.Key).ShouldBeFalse();

    void Destroy() => _server.Dispose();
}
