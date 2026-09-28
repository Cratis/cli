// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateCheckCache;

/// <summary>
/// Valid JSON of the wrong shape is a cache miss, like any other unreadable file.
/// </summary>
public class when_reading_a_file_without_packages : Specification
{
    string _directory = null!;
    string _path = null!;
    UpdateCheckCache _cache = null!;
    UpdateCheckEntry? _result;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "version-check.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, "{\"Packages\":null}");
        _cache = new(_path);
    }

    void Because()
    {
        _result = _cache.Read("Cratis.Cli");
        _cache.Store("Cratis.Cli", _ => new UpdateCheckEntry("3.20.0", DateTime.UtcNow));
    }

    void Destroy() => Directory.Delete(_directory, true);

    [Fact] void should_find_no_entry() => _result.ShouldBeNull();
    [Fact] void should_store_a_new_entry() => _cache.Read("Cratis.Cli")!.LatestVersion.ShouldEqual("3.20.0");
}
