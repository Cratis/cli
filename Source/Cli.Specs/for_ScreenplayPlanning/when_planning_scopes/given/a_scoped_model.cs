// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes.given;

public class a_scoped_model : Specification
{
    protected string _folder = null!;
    protected string _destination = null!;
    protected string _projectFile = null!;
    protected string _lookupFile = null!;
    protected string _taskFile = null!;
    private protected ScreenplayPlanning _planning = null!;
    private protected ArtifactPublisher _publisher = null!;

    protected const string ProjectSource = """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                description "Registers <project> & name"
                projectId ProjectId identifier
                name String
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                description "A project was registered"
                documentation
                  ```markdown
                  Keep the project identity.
                  ```
                projectId ProjectId
                name String
              specification Registering
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
        """;

    protected const string LookupSource = """
        module Projects
          feature Registration
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name String
              query ProjectById => ProjectSummary optional
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
        """;

    protected const string TaskSource = """
        policy Staff
          require role "Staff"
        module Tasks
          feature Backlog
            feature Intake
              slice StateChange CaptureTask
                command CaptureTask
                  authorize Staff
                  taskId Uuid identifier
                  title String
                  produces TaskCaptured
                    for taskId
                    title = title
                event TaskCaptured
                  title String
        """;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-render-scope-{Guid.NewGuid():N}")).FullName;
        _destination = Path.Combine(_folder, "out");
        _projectFile = Path.Combine(_folder, "project.play");
        _lookupFile = Path.Combine(_folder, "lookup.play");
        _taskFile = Path.Combine(_folder, "task.play");
        File.WriteAllText(_projectFile, ProjectSource);
        File.WriteAllText(_lookupFile, LookupSource);
        File.WriteAllText(_taskFile, TaskSource);
        _planning = new();
        _publisher = new();
    }

    private protected Task<ScreenplayRenderPlan> Plan(params string[] scopes) =>
        _planning.Plan(new(_folder, "Shop", "cratis") { Scope = scopes }, CancellationToken.None);

    private protected ArtifactPublicationRequest Request(ScreenplayRenderPlan plan) => new(plan.Artifacts!, _destination, false)
    {
        SemanticAddresses = plan.SemanticAddresses,
        Selection = plan.Selection,
        ScaffoldOptions = plan.ScaffoldOptions
    };

    private protected Task<ArtifactPublicationResult> Publish(ScreenplayRenderPlan plan) => _publisher.Publish(Request(plan), CancellationToken.None);

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
