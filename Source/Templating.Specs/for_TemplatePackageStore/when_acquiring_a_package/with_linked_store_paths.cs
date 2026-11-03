// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_linked_store_paths : given_a_store
{
    [Theory]
    [InlineData("Test.Pkg")]
    [InlineData("Test.Pkg/1.0.0")]
    async Task should_refuse_a_linked_destination_without_changing_its_target(string relative)
    {
        var outside = Directory.CreateDirectory(Path.Combine(FeedRoot, "outside")).FullName;
        await File.WriteAllTextAsync(Path.Combine(outside, ".cratis-complete"), "original marker", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(outside, "asset.txt"), "original", TestContext.Current.CancellationToken);
        var link = Path.Combine(StoreRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(link));
        Directory.CreateSymbolicLink(link, outside);
        try
        {
            var error = await Catch.Exception(() => Store.Acquire(Feed, "Test.Pkg", "1.0.0"));
            error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
            (await File.ReadAllTextAsync(Path.Combine(outside, "asset.txt"), TestContext.Current.CancellationToken)).ShouldEqual("original");
            (await File.ReadAllTextAsync(Path.Combine(outside, ".cratis-complete"), TestContext.Current.CancellationToken)).ShouldEqual("original marker");
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    [Theory]
    [InlineData(".cratis-complete")]
    [InlineData("template.json")]
    async Task should_refuse_linked_cache_files_before_reuse(string filename)
    {
        var destination = Directory.CreateDirectory(Path.Combine(StoreRoot, "Test.Pkg", "1.0.0")).FullName;
        await File.WriteAllTextAsync(Path.Combine(destination, ".cratis-complete"), string.Empty, TestContext.Current.CancellationToken);
        var outside = Path.Combine(FeedRoot, "original.txt");
        await File.WriteAllTextAsync(outside, "original", TestContext.Current.CancellationToken);
        var link = Path.Combine(destination, filename);
        if (File.Exists(link)) File.Delete(link);
        File.CreateSymbolicLink(link, outside);
        try
        {
            var error = await Catch.Exception(() => Store.Acquire(Feed, "Test.Pkg", "1.0.0"));
            error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
            (await File.ReadAllTextAsync(outside, TestContext.Current.CancellationToken)).ShouldEqual("original");
        }
        finally
        {
            File.Delete(link);
        }
    }

    [Fact]
    async Task should_resolve_an_explicit_store_alias_once()
    {
        Directory.CreateDirectory(StoreRoot);
        var alias = Path.Combine(FeedRoot, "store-alias");
        Directory.CreateSymbolicLink(alias, StoreRoot);
        try
        {
            var store = new TemplatePackageStore(alias);
            var installed = await store.Acquire(Feed, "Test.Pkg", "1.0.0");
            File.Exists(Path.Combine(installed, ".cratis-complete")).ShouldBeTrue();
            File.Exists(Path.Combine(StoreRoot, "Test.Pkg", "1.0.0", ".cratis-complete")).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(alias);
        }
    }
}
