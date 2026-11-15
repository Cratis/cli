// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildLevels;

public class when_reading_levels : Specification
{
    [Fact] void should_read_screenplay() => Read("screenplay").ShouldEqual(BuildLevel.Screenplay);
    [Fact] void should_read_code() => Read("code").ShouldEqual(BuildLevel.Code);
    [Fact] void should_read_code_ai() => Read("Code-AI").ShouldEqual(BuildLevel.CodeWithAI);
    [Fact] void should_read_ai() => Read("ai").ShouldEqual(BuildLevel.AI);
    [Fact] void should_refuse_what_is_not_a_level() => BuildLevels.TryParse("static", out _).ShouldBeFalse();
    [Fact] void should_name_every_level() => BuildLevels.Names.ShouldContainOnly("screenplay", "code", "code-ai", "ai");
    [Fact] void should_name_the_ai_level_as_a_kit() => BuildLevels.KitName(BuildLevel.AI, "Axon").ShouldEqual("axon-ai");
    [Fact] void should_render_only_at_the_code_levels() => new[] { BuildLevel.Screenplay, BuildLevel.Code, BuildLevel.CodeWithAI, BuildLevel.AI }.Where(BuildLevels.Renders).ShouldContainOnly(BuildLevel.Code, BuildLevel.CodeWithAI);
    [Fact] void should_need_an_agent_at_code_ai_and_ai() => new[] { BuildLevel.Screenplay, BuildLevel.Code, BuildLevel.CodeWithAI, BuildLevel.AI }.Where(BuildLevels.NeedsAgent).ShouldContainOnly(BuildLevel.CodeWithAI, BuildLevel.AI);
    [Fact] void should_map_code_ai_to_studios_static_code_refined_by_ai() => BuildLevels.StudioMode(BuildLevel.CodeWithAI).ShouldEqual("Static code refined by AI");
    [Fact] void should_map_ai_to_studios_ai_only() => BuildLevels.StudioMode(BuildLevel.AI).ShouldEqual("AI only");

    static BuildLevel? Read(string name) => BuildLevels.TryParse(name, out var level) ? level : null;
}
