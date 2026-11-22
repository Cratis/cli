// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderingDocumentation;

public class when_comparing_the_pages_with_the_bundled_toolchain : given.the_rendering_documentation
{
    string Screenplay => Versions["ScreenplayVersion"];
    string Stage => Versions["StageVersion"];

    [Fact] void should_name_the_bundled_toolchain_on_the_screenplay_page() =>
        ScreenplayPage.ShouldContain($"The CLI bundles Screenplay {Screenplay} (including its MCP server), Stage {Stage}, the Axon renderer {Versions["CratisStageAxonVersion"]}, the Arc source adapter {Versions["CratisArcScreenplayVersion"]}, and the Critter Stack source adapter {Versions["CratisCritterStackScreenplayVersion"]}.");
    [Fact] void should_name_the_critter_stack_provider_in_the_provenance_example() => ScreenplayPage.ShouldContain($"provider: critter-stack {Versions["CratisCritterStackScreenplayVersion"]}\n");
    [Fact] void should_name_the_stage_image_the_run_command_uses() => ScreenplayPage.ShouldContain($"`cratis/stage:{Stage}`");
    [Fact] void should_name_the_stage_container_in_the_validation_table() => ScreenplayPage.ShouldContain($"The Stage {Stage} container compiles");
    [Fact] void should_name_the_default_stage_tag_on_the_run_page() => RunPage.ShouldContain($"Default: `{Stage}`");
    [Fact] void should_name_the_embedded_screenplay_on_the_mcp_page() => ScreenplayMcpPage.ShouldContain($"embedded Screenplay {Screenplay} Model Context Protocol");
    [Fact] void should_name_the_bundled_stage_renderer_on_the_mcp_page() => ScreenplayMcpPage.ShouldContain($"bundled Stage {Stage} renderer");
    [Fact] void should_name_the_stage_renderer_on_the_screenplay_page() => ScreenplayPage.ShouldContain($"Stage {Stage} renders a single project");
}
