// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_superseding;

public class and_nothing_is_stored : Specification
{
    readonly a_fake_authorization_server _server = new();
    string? _failure;

    async Task Because() => _failure = await _server.Provider().Supersede(DirectTarget.Create("https://direct.example", "team"), CancellationToken.None);

    [Fact] void should_not_revoke_anything() => _server.Revoked.ShouldBeEmpty();
    [Fact] void should_report_no_failure() => _failure.ShouldBeNull();

    void Destroy() => _server.Dispose();
}
