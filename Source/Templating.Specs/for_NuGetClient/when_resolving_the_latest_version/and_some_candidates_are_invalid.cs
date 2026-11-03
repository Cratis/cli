// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_resolving_the_latest_version;

public class and_some_candidates_are_invalid : Specification
{
    [Theory]
    [InlineData("9.0.0-beta/next")]
    [InlineData("9.0.0-beta\\next")]
    [InlineData("9.0.0-beta%2fnext")]
    [InlineData("C:\\9.0.0")]
    [InlineData("\\\\host\\9.0.0")]
    [InlineData("9.0.0-beta.")]
    async Task should_select_only_a_complete_valid_version(string candidate)
    {
        var handler = new FakeHttpHandler();
        handler.ServeServiceIndex("FlatContainer");
        handler.Respond("https://flat.example/test.pkg/index.json", () => System.Text.Json.JsonSerializer.Serialize(new { versions = new[] { "1.0.0-beta.1", candidate } }));
        var version = await new NuGetClient(new HttpClient(handler)).GetLatestVersion(new NuGetFeed("test", "https://feed.example/v3/index.json"), "Test.Pkg");
        version.ShouldEqual("1.0.0-beta.1");
    }
}
