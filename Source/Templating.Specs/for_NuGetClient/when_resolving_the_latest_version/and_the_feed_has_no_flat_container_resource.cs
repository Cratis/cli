// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_resolving_the_latest_version;

public class and_the_feed_has_no_flat_container_resource : Specification
{
    FakeHttpHandler _handler = new();
    Exception? _error;

    void Establish() => _handler.Respond(
        "https://feed.example/v3/index.json",
        () => """{ "resources": [ { "@type": "SearchQueryService", "@id": "https://search.example" } ] }""");

    async Task Because() => _error = await Catch.Exception(async () => await new NuGetClient(new HttpClient(_handler)).GetVersions(
        new NuGetFeed("test", "https://feed.example/v3/index.json"), "Any"));

    [Fact] void should_report_the_missing_resource() => _error!.Message.ShouldContain("flat container");
}
