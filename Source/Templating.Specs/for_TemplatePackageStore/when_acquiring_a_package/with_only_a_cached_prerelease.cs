// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_only_a_cached_prerelease : given_a_store
{
    void Establish()
    {
        var cached = Directory.CreateDirectory(Path.Combine(StoreRoot, "Test.Pkg", "2.0.0-beta")).FullName;
        File.WriteAllText(Path.Combine(cached, ".cratis-complete"), string.Empty);
    }

    [Fact]
    async Task should_ask_an_available_feed_for_a_stable_version() =>
        (await Store.ResolveVersion(Feed, "Test.Pkg", "*")).ShouldEqual("1.1.0");

    [Fact]
    async Task should_reuse_an_exact_prerelease() =>
        (await Store.ResolveVersion(Feed, "Test.Pkg", "2.0.0-beta")).ShouldEqual("2.0.0-beta");

    [Fact]
    async Task should_fall_back_to_the_cached_prerelease_if_the_feed_is_unreachable() =>
        (await Store.ResolveVersion(new NuGetFeed("offline", Path.Combine(FeedRoot, "missing")), "Test.Pkg", "*")).ShouldEqual("2.0.0-beta");

    [Fact]
    async Task should_use_the_cached_prerelease_after_an_http_transport_failure()
    {
        using var handler = new FakeHttpHandler();
        handler.Respond("https://feed.example/v3/index.json", () => throw new HttpRequestException("feed unreachable"));
        using var client = new HttpClient(handler);
        var store = new TemplatePackageStore(StoreRoot, new NuGetClient(client));
        (await store.ResolveVersion(new NuGetFeed("offline", "https://feed.example/v3/index.json"), "Test.Pkg", "*")).ShouldEqual("2.0.0-beta");
        handler.Requests.Count.ShouldEqual(1);
    }

    [Fact]
    async Task should_not_use_the_cached_prerelease_if_the_package_is_absent_from_an_available_feed()
    {
        var empty = Directory.CreateDirectory(Path.Combine(FeedRoot, "empty")).FullName;
        var error = await Catch.Exception(() => Store.ResolveVersion(new NuGetFeed("empty", empty), "Test.Pkg", "*"));
        error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
    }
}
