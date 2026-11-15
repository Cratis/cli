// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;

namespace Cratis.Cli.for_RenderProfileVersion;

public class when_refusing_the_upgrade : Specification
{
    [Fact] void should_refuse_a_target_without_scaffold_profiles() => RenderProfileVersion.UpgradeRefusal(true, false, false)!.Code.ShouldEqual(RenderProfileVersion.UpgradeNotSupportedCode);
    [Fact] void should_refuse_a_scoped_render() => RenderProfileVersion.UpgradeRefusal(true, true, true)!.Code.ShouldEqual(RenderProfileVersion.UpgradeScopedCode);
    [Fact] void should_allow_an_unscoped_render() => RenderProfileVersion.UpgradeRefusal(true, true, false).ShouldBeNull();
    [Fact] void should_allow_a_render_without_the_opt_in() => RenderProfileVersion.UpgradeRefusal(false, true, true).ShouldBeNull();
}
