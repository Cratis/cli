// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_acquiring_a_verified_release : given.a_release_download
{
    async Task Because() => _download = await _downloads.Acquire("4.55.0", ".mcpb");

    [Fact] void should_download_only_the_matching_cratis_release_and_checksum() => _requests.ShouldContainOnly(Url + ".sha256", Url);
    [Fact] void should_store_verified_bytes() => File.ReadAllText(_download).ShouldEqual(Bytes);
    [Fact] void should_use_a_stable_native_cache_name() => Path.GetFileName(_download).ShouldEqual(Name);
}
