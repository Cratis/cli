// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Build.for_BuildChildEnvironment;

public class when_a_workload_overrides_its_environment : Specification
{
    readonly ProcessStartInfo _start = new();

    void Establish() => _start.Environment["cRaTiS_bUiLdEr_ToKeN"] = "inherited";
    void Because() => BuildChildEnvironment.Apply(_start, new Dictionary<string, string> { ["cratis_studio_mcp_secret"] = "override", ["ORDINARY"] = "preserved" });

    [Fact] void should_remove_inherited_builder_tokens() => _start.Environment.ContainsKey("cRaTiS_bUiLdEr_ToKeN").ShouldBeFalse();
    [Fact] void should_remove_secret_overrides() => _start.Environment.ContainsKey("cratis_studio_mcp_secret").ShouldBeFalse();
    [Fact] void should_keep_ordinary_overrides() => _start.Environment["ORDINARY"].ShouldEqual("preserved");
}
