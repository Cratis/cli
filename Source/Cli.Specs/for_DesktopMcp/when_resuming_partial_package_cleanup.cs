// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_DesktopMcp;

public class when_resuming_partial_package_cleanup : given.a_personal_marketplace
{
    string _folder;
    async Task Establish()
    {
        await _client.Install(_artifact, "4.55.0", _home, false);
        var state = _client.ReadOwned()!;
        _folder = Path.Combine(_home, state.Folder);
        File.Delete(Path.Combine(_folder, "plugin.json"));
        state.Files.Remove("plugin.json");
        await File.WriteAllTextAsync(_marketplace, new DesktopMcpMarketplace(await File.ReadAllTextAsync(_marketplace)).Set(state.Entry, null));
        await File.WriteAllTextAsync(Path.Combine(_home, ".cratis/mcp-desktop/chatgpt.json"), JsonSerializer.Serialize(state with { CleanupPending = true }));
    }
    async Task Because() => _result = await _client.Uninstall(false);

    [Fact] void should_finish_removing_the_remaining_owned_package() => Directory.Exists(_folder).ShouldBeFalse();
    [Fact] void should_remove_the_cleanup_receipt() => _client.ReadOwned().ShouldBeNull();
    [Fact] void should_preserve_the_foreign_plugin() => File.ReadAllText(_marketplace).ShouldContain("\"name\": \"foreign\"");
}
