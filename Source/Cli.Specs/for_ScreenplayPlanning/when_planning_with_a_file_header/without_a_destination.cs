// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_file_header;

public class without_a_destination : given.a_file_header_plan
{
    async Task Because() => _result = await PlanWithHeader();

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_plan_generated_code() => Code(_result.Artifacts!.Artifacts).ShouldNotBeEmpty();
    [Fact] void should_start_every_code_file_with_the_header() => Code(_result.Artifacts!.Artifacts).Where(artifact => !Text(artifact).StartsWith(Comment, StringComparison.Ordinal)).Select(artifact => artifact.RelativePath).ShouldBeEmpty();
    [Fact] void should_leave_the_project_file_without_a_header() => Text(_result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath == "Projects.csproj")).StartsWith("<Project", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_carry_the_header_to_the_scaffold() => _result.ScaffoldOptions!.FileHeader.ShouldEqual(Header);
}
