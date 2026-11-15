// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_backup_cleanup_failure : given_a_store
{
    void Establish()
    {
        StoreRoot = new PackageStorePath(new DirectoryInfo(StoreRoot)).Root;
        Store = new TemplatePackageStore(StoreRoot);
    }

    [Fact]
    async Task should_report_committed_success_and_leave_the_backup_in_place()
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
        var installed = await acquisition.Acquire(new DirectoryInfo(destination), () => Task.FromResult<Stream>(File.OpenRead(Path.Combine(FeedRoot, "Test.Pkg.1.1.0.nupkg"))), replace: true);
        installed.ShouldEqual(destination);
        File.Exists(Path.Combine(installed, ".cratis-complete")).ShouldBeTrue();
        Directory.Exists(backup).ShouldBeTrue();
        File.Exists(Path.Combine(FeedRoot, "Test.Pkg.1.1.0.nupkg")).ShouldBeTrue();
    }
}
