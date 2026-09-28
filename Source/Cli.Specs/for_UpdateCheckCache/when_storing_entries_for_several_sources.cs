// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateCheckCache;

public class when_storing_entries_for_several_sources : Specification
{
    string _directory = null!;
    UpdateCheckCache _cache = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _cache = new(Path.Combine(_directory, "version-check.json"));
    }

    void Because()
    {
        _cache.Store("Cratis.Cli", _ => new UpdateCheckEntry("3.20.0", DateTime.UtcNow));
        new UpdateCheckCache(Path.Combine(_directory, "version-check.json")).Store("github:Cratis/AI:compare", _ => new UpdateCheckEntry("x:1", DateTime.UtcNow));
    }

    void Destroy() => Directory.Delete(_directory, true);

    [Fact] void should_keep_the_first_entry() => _cache.Read("Cratis.Cli")!.LatestVersion.ShouldEqual("3.20.0");
    [Fact] void should_keep_the_second_entry() => _cache.Read("github:Cratis/AI:compare")!.LatestVersion.ShouldEqual("x:1");
    [Fact] void should_leave_no_temporary_files_behind() => Directory.GetFiles(_directory).Length.ShouldEqual(1);
}
