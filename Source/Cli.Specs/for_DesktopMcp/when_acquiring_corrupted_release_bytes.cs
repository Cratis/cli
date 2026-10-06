// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_acquiring_corrupted_release_bytes : given.a_release_download
{
    void Establish() => _corruptResponse = true;
    async Task Because() => _error = await Catch.Exception(() => _downloads.Acquire("4.55.0", ".mcpb"));

    [Fact] void should_reject_the_download() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_not_leave_a_cached_or_partial_file() => Directory.GetFiles(Path.Combine(_home, ".cratis/mcp-desktop/downloads")).ShouldBeEmpty();
}
