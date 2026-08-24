// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.given;

public class a_screenplay_planning : Specification
{
    protected static readonly string Source = Lines(
        "concept ProjectId : Uuid",
        "concept ProjectName : String",
        "module Projects",
        "  feature Registration",
        "    slice StateChange RegisterProject",
        "      command RegisterProject",
        "        projectId ProjectId identifier",
        "        name ProjectName",
        "        validate",
        "          name not empty message \"Project name is required\"",
        "        produces ProjectRegistered",
        "          for projectId",
        "          projectId = projectId",
        "          name = name",
        "      event ProjectRegistered",
        "        projectId ProjectId",
        "        name ProjectName",
        "      specification RegisteringAProject",
        "        when RegisterProject",
        "          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
        "          name = \"Screenplay\"",
        "        then ProjectRegistered",
        "          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
        "          name = \"Screenplay\"",
        "        then readmodel ProjectSummary",
        "          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
        "          name = \"Screenplay\"",
        "        then query ProjectById",
        "          arguments",
        "            projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
        "          result",
        "            projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
        "            name = \"Screenplay\"",
        "      specification RejectingAnEmptyProjectName",
        "        when RegisterProject",
        "          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
        "          name = \"\"",
        "        then error \"Project name is required\"",
        "    slice StateView ProjectLookup",
        "      readmodel ProjectSummary",
        "        projectId ProjectId",
        "        name ProjectName",
        "      query ProjectById => ProjectSummary?",
        "        by projectId ProjectId",
        "      projection ProjectSummaryProjection => ProjectSummary",
        "        from ProjectRegistered key projectId",
        "          name = name");

    protected string _folder = null!;
    protected string _file = null!;
    private protected ScreenplayPlanning _planning = null!;

    static string Lines(params string[] lines) => string.Join('\n', lines);

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-screenplay-plan-{Guid.NewGuid():N}")).FullName;
        _file = Path.Combine(_folder, "RegisterProject.play");
        File.WriteAllText(_file, Source);
        _planning = new ScreenplayPlanning();
    }

    private protected Task<ScreenplayRenderPlan> Plan(string? path = null, string target = "cratis") =>
        _planning.Plan(new(path ?? _file, "Projects", target), CancellationToken.None);

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
