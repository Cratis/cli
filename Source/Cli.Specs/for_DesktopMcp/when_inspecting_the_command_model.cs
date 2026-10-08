// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_inspecting_the_command_model : Specification
{
    IReadOnlyList<CommandNode> _commands;

    void Because() => _commands = CliCommandTree.Commands.Single(node => node.Name == "screenplay").Children.Single(node => node.Name == "desktop").Children;

    [Fact] void should_register_all_four_verbs() => _commands.Select(node => node.Name).ShouldContainOnly(["install", "status", "uninstall", "update"]);
    [Fact] void should_not_advertise_global_options() => _commands.All(node => !node.IncludesGlobalOptions).ShouldBeTrue();
    [Fact] void should_offer_model_root_for_install() => _commands.Single(node => node.Name == "install").Options.ShouldContain("--model-root");
    [Fact] void should_offer_model_root_for_update() => _commands.Single(node => node.Name == "update").Options.ShouldContain("--model-root");
    [Fact] void should_not_offer_model_root_for_status() => _commands.Single(node => node.Name == "status").Options.ShouldNotContain("--model-root");
    [Fact] void should_not_offer_model_root_for_uninstall() => _commands.Single(node => node.Name == "uninstall").Options.ShouldNotContain("--model-root");
}
