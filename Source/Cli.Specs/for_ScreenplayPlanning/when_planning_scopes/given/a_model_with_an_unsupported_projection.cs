// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes.given;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class a_model_with_an_unsupported_projection : a_scoped_model
{
    void Establish() => File.WriteAllText(_lookupFile, """
        module Projects
          feature Registration
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name String
                lastSeen DateTime?
              query ProjectById => ProjectSummary optional
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
                  lastSeen = $eventContext.occurred
        """);
}
