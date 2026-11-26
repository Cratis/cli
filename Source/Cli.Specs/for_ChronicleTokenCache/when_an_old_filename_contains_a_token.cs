// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_an_old_filename_contains_a_token : given.an_oauth_cache
{
    string _legacyPath;
    bool _scopedReplacement;

    void Establish()
    {
        _legacyPath = CliConfiguration.GetTokenCachePath($"{Context}_client");
        Seed(_legacyPath, new JsonObject
        {
            ["AccessToken"] = "legacy-fixture",
            ["Expiry"] = DateTimeOffset.UtcNow.AddHours(1).ToString("O")
        }.ToJsonString());
    }

    async Task Because()
    {
        await Acquire();
        _scopedReplacement = File.Exists(_path) &&
            JsonNode.Parse(await File.ReadAllTextAsync(_path))?["ScopeHash"]?.GetValue<string>() is { Length: 64 };
    }

    [Fact] void should_acquire_a_fresh_token() => _endpoint.Requests.ShouldEqual(1);
    [Fact] void should_return_the_fresh_token() => _receivedFresh.ShouldBeTrue();
    [Fact] void should_create_a_scoped_replacement_automatically() => _scopedReplacement.ShouldBeTrue();
    [Fact] void should_remove_the_unused_legacy_bearer() => File.Exists(_legacyPath).ShouldBeFalse();
}
