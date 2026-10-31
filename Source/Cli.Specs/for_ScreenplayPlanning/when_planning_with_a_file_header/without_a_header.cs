// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_file_header;

public class without_a_header : for_ScreenplayPlanning.given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await Plan();

    [Fact]
    void should_start_no_generated_csharp_file_with_a_comment() => _result.Artifacts!.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).StartsWith("//", StringComparison.Ordinal))
        .Select(artifact => artifact.RelativePath)
        .ShouldBeEmpty();
    [Fact] void should_have_no_scaffold_header() => _result.ScaffoldOptions!.FileHeader.ShouldBeEmpty();
}
