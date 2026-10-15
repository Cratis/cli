// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageRuntimeStatus;

public class when_classifying_semantic_refusals : Specification
{
    [Theory]
    [InlineData("STAGE-ESM-030: Routes run only on the semantic engine.", true)]
    [InlineData("STAGE-ESM-034: Reactions cannot run on the legacy event model or renderer; use the semantic path to preserve runsAs.", true)]
    [InlineData("Use the semantic path to preserve observer filters.", true)]
    [InlineData("Use the SEMANTIC ENGINE.", true)]
    [InlineData("STAGE-ESM-030: Command routes are not supported.", false)]
    [InlineData("History cannot be reconstructed.", false)]
    void should_classify_by_the_engine_guidance_not_the_code(string details, bool expected) =>
        new StageRuntimeIssue("Plan", "model", details).PointsToSemantic.ShouldEqual(expected);
}
