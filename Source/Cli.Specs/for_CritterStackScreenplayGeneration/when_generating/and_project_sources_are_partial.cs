// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class and_project_sources_are_partial : given.a_marten_application_built_from_source
{
    GeneratedScreenplay _result = null!;

    void Establish() => Loaded = Loaded with
    {
        Compilations = [.. Loaded.Compilations, Loaded.Compilations.Single()],
        ProjectNames = [ProjectName, "Reporting"],
        ProjectSources = [ProjectSource]
    };

    void Because() => _result = CritterStackScreenplayGeneration.GenerateFrom(
        Loaded,
        "/workspace/Banking.slnx",
        ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.Marten });

    [Fact] void should_fail_closed() => _result.Source.ShouldBeEmpty();
    [Fact] void should_report_the_stable_source_metadata_code() => _result.Diagnostics.Single().Code.ShouldEqual("CLI0018");
    [Fact] void should_report_the_mismatched_counts() => _result.Diagnostics.Single().Message.ShouldContain("1 entries for 2 compilations");
    [Fact] void should_preserve_the_loaded_project_names() => _result.Projects.ShouldContainOnly([ProjectName, "Reporting"]);
}
