// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_legacy_complete_caches : given_a_store
{
    [Theory]
    [InlineData("1.2")]
    [InlineData("01.002.0003")]
    [InlineData("1.2.3.4")]
    async Task should_reuse_the_exact_existing_generation_offline(string version)
    {
        var destination = Directory.CreateDirectory(Path.Combine(StoreRoot, "Test.Pkg", version)).FullName;
        await File.WriteAllTextAsync(Path.Combine(destination, ".cratis-complete"), "legacy marker", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(destination, "asset.txt"), "original", TestContext.Current.CancellationToken);
        var unavailable = new NuGetFeed("unavailable", Path.Combine(FeedRoot, "missing"));
        (await Store.ResolveVersion(unavailable, "Test.Pkg", "*")).ShouldEqual(version);
        var installed = await Store.Acquire(unavailable, "Test.Pkg", version);
        installed.ShouldEqual(destination);
        (await File.ReadAllTextAsync(Path.Combine(destination, ".cratis-complete"), TestContext.Current.CancellationToken)).ShouldEqual("legacy marker");
        (await File.ReadAllTextAsync(Path.Combine(destination, "asset.txt"), TestContext.Current.CancellationToken)).ShouldEqual("original");
    }

    [Fact]
    async Task should_resolve_a_cached_legacy_prerelease_only_exactly_or_when_the_feed_is_unreachable()
    {
        const string version = "1.2.3-Beta.01+Build.A";
        var destination = Directory.CreateDirectory(Path.Combine(StoreRoot, "Test.Pkg", version)).FullName;
        await File.WriteAllTextAsync(Path.Combine(destination, ".cratis-complete"), "legacy marker", TestContext.Current.CancellationToken);
        var unavailable = new NuGetFeed("unavailable", Path.Combine(FeedRoot, "missing"));
        (await Store.ResolveVersion(Feed, "Test.Pkg", "*")).ShouldEqual("1.1.0");
        (await Store.ResolveVersion(Feed, "Test.Pkg", version)).ShouldEqual(version);
        (await Store.ResolveVersion(unavailable, "Test.Pkg", "*")).ShouldEqual(version);
        (await Store.Acquire(unavailable, "Test.Pkg", version)).ShouldEqual(destination);
        (await File.ReadAllTextAsync(Path.Combine(destination, ".cratis-complete"), TestContext.Current.CancellationToken)).ShouldEqual("legacy marker");
    }
}
