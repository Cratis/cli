// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_DesktopMcp;

public class when_uninstalling_a_locked_windows_source : given.a_personal_marketplace
{
    string _locked;
    async Task Establish()
    {
        await _client.Install(_artifact, "4.55.0", _home, false);
        _locked = Path.Combine(_home, _client.ReadOwned()!.Folder, "plugin.json");
    }
    async Task Because()
    {
        await using var locked = new FileStream(_locked, FileMode.Open, FileAccess.Read, FileShare.Read);
        _error = await Catch.Exception(() => _client.Uninstall(false));
    }

    [given.windows_only.Fact] void should_report_partial_cleanup() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [given.windows_only.Fact] void should_preserve_remaining_ownership() => _client.ReadOwned()!.CleanupPending.ShouldBeTrue();
    [given.windows_only.Fact] void should_leave_the_locked_file() => File.Exists(_locked).ShouldBeTrue();
    [given.windows_only.Fact] void should_unregister_only_the_owned_entry() => JsonNode.Parse(File.ReadAllText(_marketplace))!["plugins"]!.AsArray().Single()!["name"]!.GetValue<string>().ShouldEqual("foreign");
}
