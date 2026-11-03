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

    [Fact]
    async Task should_refuse_a_linked_backup_in_place_of_a_missing_destination()
    {
        PublishPackage("Test.Pkg", "1.2.0");
        var destination = await Store.Acquire(Feed, "Test.Pkg", "1.2.0");
        var outside = Directory.CreateDirectory(Path.Combine(FeedRoot, "linked-backup-target")).FullName;
        await File.WriteAllTextAsync(Path.Combine(outside, "asset.txt"), "original", TestContext.Current.CancellationToken);
        Directory.Delete(destination, recursive: true);
        var backup = Path.Combine(StoreRoot, $".cratis-backup-{KeyOf(destination)}");
        Directory.CreateSymbolicLink(backup, outside);
        var error = await Catch.Exception(() => Store.Acquire(Feed, "Test.Pkg", "1.2.0"));
        error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        Directory.Exists(destination).ShouldBeFalse();
        new DirectoryInfo(backup).LinkTarget.ShouldNotBeNull();
        (await File.ReadAllTextAsync(Path.Combine(outside, "asset.txt"), TestContext.Current.CancellationToken)).ShouldEqual("original");
    }

    [Fact]
    async Task should_not_remove_through_a_linked_stage_directory()
    {
        PublishPackage("Test.Pkg", "1.3.0");
        var destination = await Store.Acquire(Feed, "Test.Pkg", "1.3.0");
        var outside = Directory.CreateDirectory(Path.Combine(FeedRoot, "linked-stage-target")).FullName;
        await File.WriteAllTextAsync(Path.Combine(outside, "asset.txt"), "original", TestContext.Current.CancellationToken);
        Directory.CreateSymbolicLink(Path.Combine(StoreRoot, $".cratis-stage-{KeyOf(destination)}-{Guid.NewGuid():N}"), outside);
        (await Store.Acquire(Feed, "Test.Pkg", "1.3.0")).ShouldEqual(destination);
        (await File.ReadAllTextAsync(Path.Combine(outside, "asset.txt"), TestContext.Current.CancellationToken)).ShouldEqual("original");
    }

    static string KeyOf(string destination)
    {
        var identity = PackageStorePath.Comparer.Equals("a", "A") ? destination.ToUpperInvariant() : destination;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }
}
