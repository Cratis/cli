// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_resolving_the_latest_version;

public class with_invalid_client_requests : Specification
{
    [Theory]
    [InlineData("Test.Pkg", "1.0.0-beta/next")]
    [InlineData("Test.Pkg", "1.0.0-beta%2fnext")]
    [InlineData("../Test.Pkg", "1.0.0")]
    async Task should_refuse_before_any_http_request(string id, string version)
    {
        var handler = new FakeHttpHandler();
        var error = await Catch.Exception(() => new NuGetClient(new HttpClient(handler)).DownloadPackage(
            new NuGetFeed("test", "https://feed.example/v3/index.json"), id, version));
        error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        handler.Requests.ShouldBeEmpty();
    }
}
