// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Templating.Packages;

internal sealed class PackageAcquisition(PackageStorePath paths, Action<string, string>? moveDirectory = null)
{
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(PackageStorePath.Comparer);
    readonly Action<string, string> _move = moveDirectory ?? Directory.Move;

    internal bool IsComplete(DirectoryInfo destination)
    {
        paths.CheckTree(destination);
        var marker = new FileInfo(Path.Combine(destination.FullName, ".cratis-complete"));
        paths.Check(marker);
        return marker.Exists;
    }

    internal async Task<string> Acquire(DirectoryInfo destination, Func<Task<Stream>> open, bool replace = false)
    {
        paths.Check(destination);
        var gate = _locks.GetOrAdd(destination.FullName, _ => new SemaphoreSlim(1));
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            paths.Check(new DirectoryInfo(paths.Root));
            Directory.CreateDirectory(paths.Root);
            await using var lease = await Lock(destination).ConfigureAwait(false);
            Recover(destination);
            if (!replace && IsComplete(destination)) return destination.FullName;
            return await Stage(destination, open).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    static string Key(DirectoryInfo destination)
    {
        var identity = PackageStorePath.Comparer.Equals("a", "A") ? destination.FullName.ToUpperInvariant() : destination.FullName;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    async Task<string> Stage(DirectoryInfo destination, Func<Task<Stream>> open)
    {
        var staging = new DirectoryInfo(paths.Child($".cratis-stage-{Key(destination)}-{Guid.NewGuid():N}"));
        try
        {
            staging.Create();
            await using (var package = await open().ConfigureAwait(false)) NuGetClient.ExtractNupkg(package, staging.FullName);
            paths.CheckTree(staging);
            TemplatePackageStore.DiscoverTemplates(staging.FullName);
            var marker = new FileInfo(Path.Combine(staging.FullName, ".cratis-complete"));
            paths.Check(marker);
            await File.WriteAllTextAsync(marker.FullName, string.Empty).ConfigureAwait(false);
            Commit(staging, destination);
            return destination.FullName;
        }
        finally
        {
            Remove(staging);
        }
    }

    void Commit(DirectoryInfo staging, DirectoryInfo destination)
    {
        paths.CheckTree(destination);
        paths.CheckTree(staging);
        var backup = Backup(destination);
        if (Directory.Exists(backup.FullName) || File.Exists(backup.FullName)) MoveAside(backup, destination);
        destination.Parent!.Create();
        var hadPrevious = Directory.Exists(destination.FullName);
        if (hadPrevious) _move(destination.FullName, backup.FullName);
        try
        {
            paths.Check(destination);
            _move(staging.FullName, destination.FullName);
        }
        catch
        {
            if (hadPrevious)
            {
                paths.CheckTree(backup);
                paths.Check(destination);
                _move(backup.FullName, destination.FullName);
            }
            throw;
        }
        TryRemove(backup);
    }

    void MoveAside(DirectoryInfo backup, DirectoryInfo destination)
    {
        var trash = paths.Child($".cratis-trash-{Key(destination)}-{Guid.NewGuid():N}");
        try
        {
            if (Directory.Exists(backup.FullName)) _move(backup.FullName, trash);
            else File.Move(backup.FullName, trash);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new TemplatePackageAcquisitionError("a previous package backup could not be cleaned up.");
        }
    }

    DirectoryInfo Backup(DirectoryInfo destination) => new(paths.Child($".cratis-backup-{Key(destination)}"));

    void Recover(DirectoryInfo destination)
    {
        var backup = Backup(destination);
        if (!Directory.Exists(destination.FullName) && Directory.Exists(backup.FullName))
        {
            paths.CheckTree(backup);
            paths.Check(destination);
            destination.Parent!.Create();
            _move(backup.FullName, destination.FullName);
        }
        if (IsComplete(destination)) TryRemove(backup);
        var leftovers = new DirectoryInfo(paths.Root).EnumerateDirectories($".cratis-stage-{Key(destination)}-*")
            .Concat(new DirectoryInfo(paths.Root).EnumerateDirectories($".cratis-trash-{Key(destination)}-*"));
        foreach (var directory in leftovers) TryRemove(directory);
    }

    void TryRemove(DirectoryInfo directory)
    {
        try
        {
            Remove(directory);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or TemplatePackageAcquisitionError)
        {
            // A committed package stays successful; leave unsafe or unavailable cleanup for a later attempt.
        }
    }

    void Remove(DirectoryInfo directory)
    {
        paths.CheckTree(directory);
        if (Directory.Exists(directory.FullName)) directory.Delete(recursive: true);
    }

    async Task<FileStream> Lock(DirectoryInfo destination)
    {
        var key = Key(destination);
        var file = new FileInfo(paths.Child($".cratis-lock-{key}"));
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        while (true)
        {
            paths.Check(file);
            try
            {
                return new FileStream(file.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (File.Exists(file.FullName) && System.Diagnostics.Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(30))
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
        }
    }
}
