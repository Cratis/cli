// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_resolving_the_latest_version;

public class with_complete_version_tokens : Specification
{
    [Theory]
    [InlineData("1.2", "1.2")]
    [InlineData("1.2.3.4", "1.2.3.4")]
    [InlineData("01.002.0003", "1.2.3")]
    [InlineData("01.002-Beta.01", "01.002-Beta.01")]
    [InlineData("1.2.3+Build.01", "1.2.3+Build.01")]
    [InlineData("1.2.3-Beta-2.01+Build.A-b", "1.2.3-Beta-2.01+Build.A-b")]
    async Task should_preserve_supported_spellings(string candidate, string expected)
    {
        var handler = new FakeHttpHandler();
        handler.ServeServiceIndex("FlatContainer");
        handler.ServeVersions("test.pkg", candidate);
        var version = await new NuGetClient(new HttpClient(handler)).GetLatestVersion(new NuGetFeed("test", "https://feed.example/v3/index.json"), "Test.Pkg");
        version.ShouldEqual(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3+")]
    [InlineData("1.2.3-a..b")]
    [InlineData("1.2.3+a..b")]
    [InlineData("1.2.3-a_b")]
    [InlineData("1.2.3-a%2eb")]
    [InlineData("1.2.3-a\0b")]
    [InlineData("1.2.3-a\nb")]
    [InlineData("1.2.3.4.5")]
    [InlineData("2147483648.0")]
    async Task should_report_no_valid_candidates(string candidate)
    {
        var handler = new FakeHttpHandler();
        handler.ServeServiceIndex("FlatContainer");
        handler.Respond("https://flat.example/test.pkg/index.json", () => System.Text.Json.JsonSerializer.Serialize(new { versions = new[] { candidate } }));
        var error = await Catch.Exception(() => new NuGetClient(new HttpClient(handler)).GetLatestVersion(new NuGetFeed("test", "https://feed.example/v3/index.json"), "Test.Pkg"));
        error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
    }
}
