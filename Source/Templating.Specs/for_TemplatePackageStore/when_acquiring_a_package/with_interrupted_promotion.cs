// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_interrupted_promotion : given_a_store
{
    void Establish()
    {
        StoreRoot = new PackageStorePath(new DirectoryInfo(StoreRoot)).Root;
        Store = new TemplatePackageStore(StoreRoot);
    }

    [Fact]
    async Task should_restore_this_destinations_backup_before_reusing_it_offline()
    {
        var destination = await Store.Acquire(Feed, "Test.Pkg", "1.0.0");
        var identity = PackageStorePath.Comparer.Equals("a", "A") ? destination.ToUpperInvariant() : destination;
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        var backup = Path.Combine(StoreRoot, $".cratis-backup-{key}");
        Directory.Move(destination, backup);
        var stage = Directory.CreateDirectory(Path.Combine(StoreRoot, $".cratis-stage-{key}-{Guid.NewGuid():N}")).FullName;
        var unrelated = Directory.CreateDirectory(Path.Combine(StoreRoot, ".cratis-stage-other")).FullName;
        var unavailable = new NuGetFeed("offline", Path.Combine(FeedRoot, "missing"));
        (await Store.Acquire(unavailable, "Test.Pkg", "1.0.0")).ShouldEqual(destination);
        File.Exists(Path.Combine(destination, ".cratis-complete")).ShouldBeTrue();
        Directory.Exists(backup).ShouldBeFalse();
        Directory.Exists(stage).ShouldBeFalse();
        Directory.Exists(unrelated).ShouldBeTrue();
    }

    [Fact]
    async Task should_remove_only_this_destinations_stale_backup_after_committed_success()
    {
        var destination = await Store.Acquire(Feed, "Test.Pkg", "1.1.0");
        var identity = PackageStorePath.Comparer.Equals("a", "A") ? destination.ToUpperInvariant() : destination;
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        var backup = Directory.CreateDirectory(Path.Combine(StoreRoot, $".cratis-backup-{key}")).FullName;
        var other = Directory.CreateDirectory(Path.Combine(StoreRoot, ".cratis-backup-other")).FullName;
        (await Store.Acquire(Feed, "Test.Pkg", "1.1.0")).ShouldEqual(destination);
        Directory.Exists(backup).ShouldBeFalse();
        Directory.Exists(other).ShouldBeTrue();
    }
}
