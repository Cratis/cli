// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_WindowsDirectSecrets.given;

namespace Cratis.Cli.for_WindowsDirectSecrets;

public class when_storing_an_oversized_token_set : Specification
{
    readonly a_fake_credential_api _api = new();
    DirectTokens _recovered = null!;

    async Task Because()
    {
        var store = new WindowsDirectSecrets(_api);
        var tokens = new DirectTokens(new string('a', 6000), "short-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read");
        await store.Write("target", System.Text.Json.JsonSerializer.Serialize(tokens), CancellationToken.None);
        var restarted = new WindowsDirectSecrets(_api);
        _recovered = System.Text.Json.JsonSerializer.Deserialize<DirectTokens>((await restarted.Read("target", CancellationToken.None))!)!;
    }

    [Fact] void should_store_a_blob_below_the_windows_limit() => (_api.LargestBlob <= 2560).ShouldBeTrue();
    [Fact] void should_preserve_the_refresh_token() => _recovered.RefreshToken.ShouldEqual("short-refresh");
    [Fact] void should_reobtain_access_after_restart() => _recovered.AccessToken.ShouldBeEmpty();
}
