// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_installing_over_a_foreign_entry : given.a_personal_marketplace
{
    const string Original = "{\"name\":\"mine\",\"plugins\":[{\"name\":\"cratis-screenplay\",\"source\":{\"path\":\"./mine\"}}]}";
    void Establish() => File.WriteAllText(_marketplace, Original);
    async Task Because() => _error = await Catch.Exception(() => _client.Install(_artifact, "4.55.0", _home, false));

    [Fact] void should_refuse_the_foreign_entry() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_preserve_the_original_bytes() => File.ReadAllText(_marketplace).ShouldEqual(Original);
    [Fact] void should_not_extract_a_package() => Directory.Exists(Path.Combine(_home, ".codex")).ShouldBeFalse();
}
