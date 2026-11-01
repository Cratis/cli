// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning.when_axon_plan_has_unexecutable_slices.given;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class an_axon_target_with_an_unsupported_projection : for_AxonRenderTarget.given.an_axon_target
{
    void Establish()
    {
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([_target]));
        File.WriteAllText(_file, """
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId ProjectId identifier
                    name String
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      name = name
                  event ProjectRegistered
                    projectId ProjectId
                    name String
                slice StateView ProjectLookup
                  readmodel ProjectSummary
                    projectId ProjectId
                    name String
                    correlation Uuid?
                  query ProjectById => ProjectSummary optional
                    by projectId ProjectId
                  projection ProjectSummaryProjection => ProjectSummary
                    from ProjectRegistered key projectId
                      name = name
                      correlation = $eventContext.correlationId
            """);
    }
}
