// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_publication_recovery_is_incomplete : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    Exception _error = null!;

    void Establish()
    {
        _server.RefusedTokens.Add("new-refresh");
        _server.DeleteFailure = new IOException("store unavailable");
    }

    async Task Because()
    {
        var provider = _server.Provider();
        _error = await Catch.Exception(() => provider.Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None, () => throw new IOException("config cannot be replaced")));
    }

    [Fact] void should_report_incomplete_recovery() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_report_the_failed_revocation() => _error.Message.ShouldContain("could not be revoked");
    [Fact] void should_report_the_failed_local_cleanup() => _error.Message.ShouldContain("could not be removed");
    [Fact] void should_not_expose_the_token() => _error.Message.Contains("new-refresh", StringComparison.Ordinal).ShouldBeFalse();

    void Destroy() => _server.Dispose();
}
