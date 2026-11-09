// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_the_kit_does_not_build_the_slice : given.a_project_repository
{
    JsonObject _automation;
    JsonObject _ui;

    async Task Because()
    {
        _automation = await Build(BuildGitInstructions.Offline, given.an_order.Parse(type: "automation"));
        _ui = await Build(BuildGitInstructions.Offline, given.an_order.Parse(part: "ui"));
    }

    [Fact] void should_refuse_a_slice_type_it_does_not_build() => _automation["error"]!.GetValue<string>().ShouldContain("does not build automation slices");
    [Fact] void should_say_trying_again_will_not_help() => _automation["retryable"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_refuse_a_user_interface() => _ui["error"]!.GetValue<string>().ShouldContain("does not build user interfaces");
    [Fact] void should_run_nothing() => _log.ShouldBeEmpty();
}
