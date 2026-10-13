// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_WindowsDirectSecrets.given;

namespace Cratis.Cli.for_WindowsDirectSecrets;

public class when_refresh_credentials_exceed_the_blob_limit : Specification
{
    readonly a_fake_credential_api _api = new();
    Exception? _error;
    string? _previous;

    async Task Because()
    {
        var store = new WindowsDirectSecrets(_api);
        await store.Write("target", JsonSerializer.Serialize(new DirectTokens("access", "original", DateTimeOffset.UtcNow.AddHours(1), "direct:read")), CancellationToken.None);
        _error = await Catch.Exception(() => store.Write("target", JsonSerializer.Serialize(new DirectTokens("access", new string('r', 3000), DateTimeOffset.UtcNow.AddHours(1), "direct:read")), CancellationToken.None));
        _previous = await new WindowsDirectSecrets(_api).Read("target", CancellationToken.None);
    }

    [Fact] void should_report_the_credential_limit() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_not_replace_the_previous_refresh_token() => JsonSerializer.Deserialize<DirectTokens>(_previous!)!.RefreshToken.ShouldEqual("original");
    [Fact] void should_not_call_credential_manager_with_an_oversized_blob() => _api.Writes.ShouldEqual(1);
}
