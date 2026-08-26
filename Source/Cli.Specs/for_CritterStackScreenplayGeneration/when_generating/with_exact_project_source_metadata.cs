// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class with_exact_project_source_metadata : given.a_marten_application_built_from_source
{
    GeneratedScreenplay _result = null!;

    void Establish() => Loaded = Loaded with
    {
        ProjectSources = [ProjectSource]
    };

    void Because() => _result = CritterStackScreenplayGeneration.GenerateFrom(
        Loaded,
        "/workspace/Banking/Banking.csproj",
        ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.Marten });

    [Fact] void should_generate_the_screenplay() => _result.Source.ShouldContain("readmodel Account");
    [Fact] void should_apply_the_explicit_flat_placement_compatibility() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain("DOTNETSP0004");
    [Fact] void should_not_report_invalid_source_metadata() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain("CLI0018");
    [Fact] void should_preserve_the_loaded_project_name() => _result.Projects.ShouldContainOnly([ProjectName]);
}
