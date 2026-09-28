// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Represents the file the update checks record their answers in, one entry per source.
/// </summary>
/// <param name="path">The path of the cache file.</param>
/// <remarks>
/// Several checks run side by side at startup, and several CLI processes can run at once, each owning one key.
/// Within a process, an entry is stored by re-reading the file, changing only its own key and replacing the file
/// in one move under a lock, so a reader never sees a half-written file and no answer from the same process is
/// lost. Across processes the guarantee is deliberately weaker: there is no cross-process lock, so two CLI
/// processes storing at the same moment can each re-read the file before the other replaces it, and one of the
/// two entries is lost. On Windows, a replace that fails because another process holds the file open is dropped
/// the same way. Either case only costs one extra request to the source on a later run, which is not worth lock
/// files that a crashed process could leave behind.
/// </remarks>
internal sealed class UpdateCheckCache(string path)
{
    static readonly Lock _lock = new();
    static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Reads the entry recorded for a key.
    /// </summary>
    /// <param name="key">The key the entry is stored under.</param>
    /// <returns>The entry, or null when there is none or the file cannot be read.</returns>
    public UpdateCheckEntry? Read(string key) =>
        ReadAll().TryGetValue(key, out var entry) ? entry : null;

    /// <summary>
    /// Stores the entry for a key, keeping every other entry in the file.
    /// </summary>
    /// <param name="key">The key the entry is stored under.</param>
    /// <param name="update">Produces the new entry from the one currently recorded, if any.</param>
    /// <param name="supersedes">A key prefix whose other entries the new one replaces, or null to keep them all.</param>
    /// <remarks>
    /// A source whose key names what it was asked about - the Cratis AI comparison is keyed by the installed
    /// commit - would otherwise leave an entry behind for every installation ever checked.
    /// </remarks>
    public void Store(string key, Func<UpdateCheckEntry?, UpdateCheckEntry> update, string? supersedes = null)
    {
        lock (_lock)
        {
            var packages = ReadAll();
            var entry = update(packages.TryGetValue(key, out var existing) ? existing : null);
            if (supersedes is not null)
            {
                foreach (var superseded in packages.Keys.Where(other => other != key && other.StartsWith(supersedes, StringComparison.Ordinal)).ToList())
                {
                    packages.Remove(superseded);
                }
            }

            packages[key] = entry;
            Write(new UpdateCheckFile { Packages = packages });
        }
    }

    static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Leaving a stray temporary file behind is harmless; the cache itself is untouched.
        }
    }

    Dictionary<string, UpdateCheckEntry> ReadAll()
    {
        try
        {
            var file = File.Exists(path) ? JsonSerializer.Deserialize<UpdateCheckFile>(File.ReadAllText(path)) : null;

            // Valid JSON of the wrong shape - no packages, or a package with no entry - is a cache miss like any
            // other unreadable file, never a fault.
            return file?.Packages?
                .Where(package => package.Value is not null)
                .ToDictionary(package => package.Key, package => package.Value) ?? [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // An unreadable or corrupt cache is a cache miss: the check asks the source and writes a fresh file.
            return [];
        }
    }

    void Write(UpdateCheckFile cache)
    {
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(cache, _jsonOptions));
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A cache that cannot be written - including a replace refused because another process holds the file -
            // only means the next run asks the source again. The temporary file is removed so it does not pile up.
            TryDelete(temporary);
        }
    }

    sealed record UpdateCheckFile
    {
        public Dictionary<string, UpdateCheckEntry>? Packages { get; set; } = [];
    }
}
