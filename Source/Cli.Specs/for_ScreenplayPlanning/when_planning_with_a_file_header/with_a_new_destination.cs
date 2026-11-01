// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_file_header;

public class with_a_new_destination : given.a_file_header_plan
{
    CratisPlanResult _scaffold = null!;

    async Task Because()
    {
        _result = await PlanWithHeader(Path.Combine(_folder, "out"));
        _scaffold = CratisRendering.PlanScaffold(_result.ScaffoldOptions!);
    }

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_start_every_code_file_with_the_header() => Code(_result.Artifacts!.Artifacts).Where(artifact => !Text(artifact).StartsWith(Comment, StringComparison.Ordinal)).Select(artifact => artifact.RelativePath).ShouldBeEmpty();
    [Fact] void should_carry_the_header_in_the_scaffold_profile() => _result.ScaffoldOptions!.Profile!.Inputs.Any(input => input.Name == FileHeaderInput.Name).ShouldBeTrue();
    [Fact] void should_not_repeat_the_header_as_a_scaffold_option() => _result.ScaffoldOptions!.FileHeader.ShouldBeEmpty();
    [Fact] void should_plan_the_scaffold() => _scaffold.Success.ShouldBeTrue();
    [Fact] void should_start_every_scaffold_code_file_with_the_header() => Code(_scaffold.Artifacts).Where(artifact => !Text(artifact).StartsWith(Comment, StringComparison.Ordinal)).Select(artifact => artifact.RelativePath).ShouldBeEmpty();
}
