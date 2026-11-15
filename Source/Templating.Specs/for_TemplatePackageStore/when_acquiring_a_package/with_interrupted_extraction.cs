// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_interrupted_extraction : given_a_store
{
    [Fact]
    async Task should_remove_partial_staging_and_preserve_the_previous_generation()
    {
        var paths = new PackageStorePath(new DirectoryInfo(StoreRoot));
        var destination = paths.Child(Path.Combine("Test.Pkg", "1.0.0"));
        Directory.CreateDirectory(destination);
        await File.WriteAllTextAsync(Path.Combine(destination, "original.txt"), "original", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(destination, ".cratis-complete"), "legacy", TestContext.Current.CancellationToken);
        var sawFirstEntry = false;
        var stream = CreatePackage(() => sawFirstEntry = Directory.EnumerateDirectories(StoreRoot, ".cratis-stage-*").Any(stage => File.Exists(Path.Combine(stage, "first.txt"))));
        var error = await Catch.Exception(() => new PackageAcquisition(paths).Acquire(new DirectoryInfo(destination), () => Task.FromResult<Stream>(stream), replace: true));
        error.ShouldBeOfExactType<IOException>();
        sawFirstEntry.ShouldBeTrue();
        stream.CanRead.ShouldBeFalse();
        (await File.ReadAllTextAsync(Path.Combine(destination, "original.txt"), TestContext.Current.CancellationToken)).ShouldEqual("original");
        (await File.ReadAllTextAsync(Path.Combine(destination, ".cratis-complete"), TestContext.Current.CancellationToken)).ShouldEqual("legacy");
        Directory.GetDirectories(StoreRoot, ".cratis-stage-*").ShouldBeEmpty();
    }

    static InterruptedPackage CreatePackage(Action onInterrupted)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var writer = new StreamWriter(archive.CreateEntry("first.txt", CompressionLevel.NoCompression).Open())) writer.Write("first payload");
            using (var writer = new StreamWriter(archive.CreateEntry("second.txt", CompressionLevel.NoCompression).Open())) writer.Write("second-payload-unique");
        }
        var bytes = stream.ToArray();
        var offset = bytes.AsSpan().IndexOf(Encoding.UTF8.GetBytes("second-payload-unique"));
        return new InterruptedPackage(bytes, offset, onInterrupted);
    }

    sealed class InterruptedPackage(byte[] bytes, int offset, Action onInterrupted) : MemoryStream(bytes)
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            Interrupt();
            return base.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            Interrupt();
            return base.Read(buffer);
        }

        void Interrupt()
        {
            if (Position != offset) return;
            onInterrupted();
            throw new IOException("package stream interrupted");
        }
    }
}
