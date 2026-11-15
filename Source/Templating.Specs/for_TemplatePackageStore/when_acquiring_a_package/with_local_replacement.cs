// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_local_replacement : given_a_store
{
    [Fact]
    void should_keep_an_existing_local_install_after_invalid_replacement()
    {
        var package = Path.Combine(FeedRoot, "invalid-replacement.nupkg");
        File.Copy(Path.Combine(FeedRoot, "Test.Pkg.1.0.0.nupkg"), package);
        var installed = Store.InstallLocal(package);
        var sentinel = Path.Combine(installed, "original.txt");
        File.WriteAllText(sentinel, "original");
        File.WriteAllText(package, "not a package");
        Catch.Exception(() => Store.InstallLocal(package)).ShouldNotBeNull();
        File.ReadAllText(sentinel).ShouldEqual("original");
        Directory.GetDirectories(StoreRoot, ".cratis-stage-*").ShouldBeEmpty();
    }

    [Fact]
    void should_replace_only_after_valid_staging()
    {
        var package = Path.Combine(FeedRoot, "valid-replacement.nupkg");
        File.Copy(Path.Combine(FeedRoot, "Test.Pkg.1.0.0.nupkg"), package);
        var installed = Store.InstallLocal(package);
        File.WriteAllText(Path.Combine(installed, "old.txt"), "original");
        using (var archive = ZipFile.Open(package, ZipArchiveMode.Update))
        {
            using var writer = new StreamWriter(archive.CreateEntry("new.txt").Open());
            writer.Write("replacement");
        }
        Store.InstallLocal(package).ShouldEqual(installed);
        File.ReadAllText(Path.Combine(installed, "new.txt")).ShouldEqual("replacement");
        File.Exists(Path.Combine(installed, "old.txt")).ShouldBeFalse();
        File.Exists(Path.Combine(installed, ".cratis-complete")).ShouldBeTrue();
        Directory.GetDirectories(StoreRoot, ".cratis-backup-*").ShouldBeEmpty();
    }

    [Fact]
    async Task should_restore_a_previous_directory_after_promotion_failure()
    {
        var paths = new PackageStorePath(new DirectoryInfo(StoreRoot));
        var destination = paths.Child(Path.Combine("local", "Test.Pkg.1.0.0"));
        Directory.CreateDirectory(destination);
        await File.WriteAllTextAsync(Path.Combine(destination, "original.txt"), "original", TestContext.Current.CancellationToken);
        var acquisition = new PackageAcquisition(paths, (source, target) =>
        {
            if (Path.GetFileName(source).StartsWith(".cratis-stage-", StringComparison.Ordinal)) throw new IOException("promotion unavailable");
            Directory.Move(source, target);
        });
        var error = await Catch.Exception(() => acquisition.Acquire(new DirectoryInfo(destination), () => Task.FromResult<Stream>(File.OpenRead(Path.Combine(FeedRoot, "Test.Pkg.1.0.0.nupkg"))), replace: true));
        error.ShouldBeOfExactType<IOException>();
        (await File.ReadAllTextAsync(Path.Combine(destination, "original.txt"), TestContext.Current.CancellationToken)).ShouldEqual("original");
        Directory.GetDirectories(StoreRoot, ".cratis-stage-*").ShouldBeEmpty();
        Directory.GetDirectories(StoreRoot, ".cratis-backup-*").ShouldBeEmpty();
    }
}
