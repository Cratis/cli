// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_a_model_with_strings_files : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    string _model = null!;

    void Establish()
    {
        _model = Directory.CreateDirectory(Path.Combine(_folder, "localized")).FullName;
        File.WriteAllText(
            Path.Combine(_model, "Projects.play"),
            Source.Replace("concept ProjectName : String", "concept ProjectName : String\n  validate\n    not empty message $strings.projects.nameRequired", StringComparison.Ordinal)
                .Replace("        validate\n          name not empty message \"Project name is required\"\n", string.Empty, StringComparison.Ordinal)
                .Replace("then error \"Project name is required\"", "then error \"$strings.projects.nameRequired\"", StringComparison.Ordinal));
        File.WriteAllText(Path.Combine(_model, "Projects.en.strings"), "projects.nameRequired = \"Project name is required\"\n");
        File.WriteAllText(Path.Combine(_model, "Projects.nb.strings"), "projects.nameRequired = \"Prosjektnavn er p\u00e5krevd\"\n");
    }

    async Task Because() => _result = await Plan(_model);

    [Fact] void should_not_refuse_the_localized_validation_message() => _result.Diagnostics.Where(_ => string.Equals(_.Code, "STAGE-ESM-002", StringComparison.Ordinal) || string.Equals(_.Code, "STAGE-ESM-005", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_render_the_strings_catalog() => _result.Artifacts!.Artifacts.Any(_ => _.RelativePath.EndsWith("GeneratedStrings.cs", StringComparison.Ordinal)).ShouldBeTrue();
}
