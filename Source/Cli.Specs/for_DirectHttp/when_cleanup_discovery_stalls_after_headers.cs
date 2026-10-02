// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectHttp;

public class when_cleanup_discovery_stalls_after_headers : given.a_stalled_response
{
    Exception _error = null!;

    async Task Because()
    {
        var store = Substitute.For<IDirectSecretStore>();
        store.Read(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(null));
        store.Write(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException(new IOException("store unavailable")));
        var provider = Provider(store);
        _error = await Catch.Exception(() => provider.Replace(Target, new DirectTokens("access", "refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact] void should_cancel_the_stalled_cleanup_body_read() => BodyWasCanceled.ShouldBeTrue();
    [Fact] void should_report_incomplete_recovery_instead_of_hanging() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_report_the_unrevoked_new_token_without_exposing_it() => _error.Message.ShouldContain("could not be revoked");
}
