// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_a_folder_composed_through_imports : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    string _model = null!;

    void Establish()
    {
        _model = Directory.CreateDirectory(Path.Combine(_folder, "imported")).FullName;
        Directory.CreateDirectory(Path.Combine(_model, "projects", "registration"));
        File.WriteAllText(Path.Combine(_model, "application.play"), "domain Projects\n");
        File.WriteAllText(Path.Combine(_model, "projects", "projects.play"), "module Projects\n  import \"registration/registration.play\"\n");
        File.WriteAllText(Path.Combine(_model, "projects", "registration", "registration.play"), "feature Registration\n  import \"register-project.play\"\n");
        File.WriteAllText(
            Path.Combine(_model, "projects", "registration", "register-project.play"),
            string.Join('\n', Source.Split('\n').Skip(4).Select(line => line.Length > 4 ? line[4..] : line)) + "\n");
        File.WriteAllText(Path.Combine(_model, "shared.play"), "concept ProjectId : Uuid\nconcept ProjectName : String\n");
    }

    async Task Because() => _result = await Plan(_model);

    [Fact] void should_not_report_the_imported_files_as_top_level_documents() => _result.Diagnostics.Where(_ => _.Code == "PLAY0001").ShouldBeEmpty();
    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
}
