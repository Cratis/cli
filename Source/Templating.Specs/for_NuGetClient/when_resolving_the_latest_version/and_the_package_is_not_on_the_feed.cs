// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_resolving_the_latest_version;

public class and_the_package_is_not_on_the_feed : Specification
{
    FakeHttpHandler _handler = new();
    Exception? _error;

    void Establish() => _handler.ServeServiceIndex("PackageBaseAddress/3.0.0");

    async Task Because() => _error = await Catch.Exception(async () => await new NuGetClient(new HttpClient(_handler)).GetLatestVersion(
        new NuGetFeed("test", "https://feed.example/v3/index.json"), "Missing.Package"));

    [Fact] void should_name_the_package_and_the_feed() => _error!.Message.ShouldContain("Missing.Package");

    [Fact] void should_be_an_acquisition_error() => _error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
}
