// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_handing_a_bundle_to_claude : given.a_personal_marketplace
{
    ClaudeDesktopMcp _claude;
    string _opened;
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_home, "Applications", "Claude.app"));
        _claude = new(_platform, (_, path) => _opened = path);
    }
    async Task Because() => _result = await _claude.Install(_artifact, "4.55.0", null, false);

    [Fact] void should_open_the_verified_artifact() => _opened.ShouldEqual(_artifact);
    [Fact] void should_not_claim_host_installation() => _claude.Inspect("4.56.0").ShouldContain("awaiting host confirmation");
    [Fact] void should_report_a_new_bundle_version() => _claude.Inspect("4.56.0").ShouldContain("available update 4.56.0");
    [Fact] void should_not_touch_private_host_storage() => Directory.Exists(Path.Combine(_home, "Library")).ShouldBeFalse();
}
