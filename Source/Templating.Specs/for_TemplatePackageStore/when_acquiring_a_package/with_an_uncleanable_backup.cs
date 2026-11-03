// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_an_uncleanable_backup : given_a_store
{
    void Establish()
    {
        StoreRoot = new PackageStorePath(new DirectoryInfo(StoreRoot)).Root;
        Store = new TemplatePackageStore(StoreRoot);
    }

    [Fact]
    async Task should_still_replace_the_package_a_second_time()
    {
        var destination = await Store.Acquire(Feed, "Test.Pkg", "1.0.0");
        var paths = new PackageStorePath(new DirectoryInfo(StoreRoot));
        string? backup = null;
        var acquisition = new PackageAcquisition(paths, (source, target) =>
        {
            Directory.Move(source, target);
            if (Path.GetFileName(target).StartsWith(".cratis-backup-", StringComparison.Ordinal)) backup = target;
            else if (backup is not null) Directory.CreateSymbolicLink(Path.Combine(backup, "retained"), FeedRoot);
        });
        Task<Stream> Open() => Task.FromResult<Stream>(File.OpenRead(Path.Combine(FeedRoot, "Test.Pkg.1.1.0.nupkg")));
        await acquisition.Acquire(new DirectoryInfo(destination), Open, replace: true);
        Directory.Exists(backup).ShouldBeTrue();

        var second = await new PackageAcquisition(paths).Acquire(new DirectoryInfo(destination), Open, replace: true);

        second.ShouldEqual(destination);
        File.Exists(Path.Combine(destination, ".cratis-complete")).ShouldBeTrue();
        File.Exists(Path.Combine(FeedRoot, "Test.Pkg.1.1.0.nupkg")).ShouldBeTrue();
    }
}
