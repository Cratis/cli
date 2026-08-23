// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class and_project_sources_are_extra : given.an_application_scope
{
    GeneratedScreenplay _result;

    async Task Because()
    {
        var loaded = LoadedFrom(Project("Application", [], false, ArcSource)) with
        {
            ProjectSources = [SourceFor("Application"), SourceFor("Duplicate")]
        };

        _result = await Generate(loaded, ScreenplayProviders.Arc);
    }

    [Fact] void should_fail_closed() => _result.Source.ShouldBeEmpty();
    [Fact] void should_report_the_stable_source_metadata_code() => _result.Diagnostics.Single().Code.ShouldEqual("CLI0018");
    [Fact] void should_report_the_mismatched_counts() => _result.Diagnostics.Single().Message.ShouldContain("2 entries for 1 compilations");
    [Fact] void should_preserve_the_loaded_project_name() => _result.Projects.ShouldContainOnly(["Application"]);
}
