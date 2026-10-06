// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_reusing_a_verified_cached_release : given.a_release_download
{
    string _original;
    async Task Establish()
    {
        _original = await _downloads.Acquire("4.55.0", ".mcpb");
        _requests.Clear();
    }
    async Task Because() => _download = await _downloads.Acquire("4.55.0", ".mcpb");

    [Fact] void should_recheck_the_publisher_checksum() => _requests.ShouldContainOnly(Url + ".sha256");
    [Fact] void should_reuse_the_same_file() => _download.ShouldEqual(_original);
    [Fact] void should_not_create_duplicate_artifacts() => Directory.GetFiles(Path.GetDirectoryName(_download)!).Length.ShouldEqual(1);
}
