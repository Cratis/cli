// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_file_header.given;

public class a_file_header_plan : for_ScreenplayPlanning.given.a_screenplay_planning
{
    protected const string Comment = "// Copyright (c) Acme.\n//\n// Proprietary.\n\n";
    protected static readonly ImmutableArray<string> Header = ["Copyright (c) Acme.", "", "Proprietary."];

    private protected ScreenplayRenderPlan _result = null!;

    private protected Task<ScreenplayRenderPlan> PlanWithHeader(string? destination = null) =>
        _planning.Plan(new(_file, "Projects", "cratis") { FileHeader = Header, Destination = destination }, CancellationToken.None);

    protected static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());

    protected static IEnumerable<PlannedArtifact> Code(IEnumerable<PlannedArtifact> artifacts) =>
        artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) || artifact.RelativePath.EndsWith(".ts", StringComparison.Ordinal) || artifact.RelativePath.EndsWith(".tsx", StringComparison.Ordinal));
}
