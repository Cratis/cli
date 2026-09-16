// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_resolving_the_latest_version;

public class and_stable_versions_are_available : Specification
{
    FakeHttpHandler _handler = new();
    string? _version;

    void Establish()
    {
        _handler.ServeServiceIndex("PackageBaseAddress/3.0.0");
        _handler.ServeVersions("cratis.templates", "1.0.0", "2.0.0-rc.1", "1.3.0", "2.0.0");
    }

    async Task Because() => _version = await new NuGetClient(new HttpClient(_handler)).GetLatestVersion(
        new NuGetFeed("test", "https://feed.example/v3/index.json"), "Cratis.Templates");

    [Fact] void should_prefer_the_highest_stable_version() => _version.ShouldEqual("2.0.0");
}
