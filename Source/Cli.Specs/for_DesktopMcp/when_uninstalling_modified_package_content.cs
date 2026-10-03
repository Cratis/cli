// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_uninstalling_modified_package_content : given.a_personal_marketplace
{
    string _before;
    async Task Establish()
    {
        await _client.Install(_artifact, "4.55.0", _home, false);
        await File.WriteAllTextAsync(Path.Combine(_home, _client.ReadOwned()!.Folder, "plugin.json"), "user edited");
        _before = await File.ReadAllTextAsync(_marketplace);
    }
    async Task Because() => _error = await Catch.Exception(() => _client.Uninstall(false));

    [Fact] void should_refuse_removal() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_preserve_registration() => File.ReadAllText(_marketplace).ShouldEqual(_before);
    [Fact] void should_preserve_ownership() => _client.ReadOwned().ShouldNotBeNull();
}
