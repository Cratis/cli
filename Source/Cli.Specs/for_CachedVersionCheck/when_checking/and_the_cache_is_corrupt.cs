// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_checking;

/// <summary>
/// A torn or corrupt file is a cache miss, never a failure.
/// </summary>
public class and_the_cache_is_corrupt : given.a_cache
{
    string? _result;

    void Establish()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, "{ \"Packages\": { \"Cratis.Cli\": ");
    }

    async Task Because()
    {
        _result = await Check(_ => Task.FromResult<string?>("3.20.0"));
    }

    [Fact] void should_ask_the_source() => _result.ShouldEqual("3.20.0");
    [Fact] void should_replace_the_file_with_a_readable_one() => _cache.Read(Key)!.LatestVersion.ShouldEqual("3.20.0");
}
