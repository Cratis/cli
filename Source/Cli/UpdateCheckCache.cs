// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Represents the file the update checks record their answers in, one entry per source.
/// </summary>
/// <param name="path">The path of the cache file.</param>
/// <remarks>
/// Several checks run side by side at startup, and several CLI processes can run at once, each owning one key.
/// An entry is stored by re-reading the file, changing only its own key and replacing the file in one move, so
/// a reader never sees a half-written file and a writer keeps what others wrote before it. Two processes storing
/// at the same instant can still lose one of the two answers; that costs one extra request on a later run, which
/// is why there is no cross-process lock.
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
        ReadAll().Packages.TryGetValue(key, out var entry) ? entry : null;

    /// <summary>
    /// Stores the entry for a key, keeping every other entry in the file.
    /// </summary>
    /// <param name="key">The key the entry is stored under.</param>
    /// <param name="update">Produces the new entry from the one currently recorded, if any.</param>
    public void Store(string key, Func<UpdateCheckEntry?, UpdateCheckEntry> update)
    {
        lock (_lock)
        {
            var cache = ReadAll();
            cache.Packages[key] = update(cache.Packages.TryGetValue(key, out var existing) ? existing : null);
            Write(cache);
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

    UpdateCheckFile ReadAll()
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<UpdateCheckFile>(File.ReadAllText(path)) ?? new()
                : new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // An unreadable or corrupt cache is a cache miss: the check asks the source and writes a fresh file.
            return new();
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
            // A cache that cannot be written only means the next run asks the source again.
            TryDelete(temporary);
        }
    }

    sealed record UpdateCheckFile
    {
        public Dictionary<string, UpdateCheckEntry> Packages { get; set; } = [];
    }
}
