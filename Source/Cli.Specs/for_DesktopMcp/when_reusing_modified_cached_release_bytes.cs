// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_reusing_modified_cached_release_bytes : given.a_release_download
{
    async Task Establish()
    {
        _download = await _downloads.Acquire("4.55.0", ".mcpb");
        await File.WriteAllTextAsync(_download, "modified cache");
        _requests.Clear();
    }
    async Task Because() => _error = await Catch.Exception(() => _downloads.Acquire("4.55.0", ".mcpb"));

    [Fact] void should_reject_the_modified_cache() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_not_silently_replace_untrusted_bytes() => File.ReadAllText(_download).ShouldEqual("modified cache");
    [Fact] void should_only_read_the_publisher_checksum() => _requests.ShouldContainOnly(Url + ".sha256");
}
