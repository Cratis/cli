// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class with_source_structure_options : given.an_application_scope
{
    GeneratedScreenplay _result;

    async Task Because() => _result = await GenerateWithOptions(
        LoadedFrom(Project("Persistence", MartenPackages, false, MartenSource)),
        ScreenplayGenerationOptions.Default with
        {
            Provider = ScreenplayProviders.Marten,
            FeatureRoot = @"Features\.//",
            Module = "Lending",
            SegmentsToSkip = 2
        });

    [Fact] void should_report_the_application_role() => _result.Provenance.Projects.Single().SourceStructure.ProjectRole.ShouldEqual("Application");
    [Fact] void should_report_the_policy_version() => _result.Provenance.Projects.Single().SourceStructure.PolicyVersion.ShouldEqual(1);
    [Fact] void should_report_the_feature_root() => _result.Provenance.Projects.Single().SourceStructure.FeatureRoot.ShouldEqual("Features");
    [Fact] void should_report_the_module() => _result.Provenance.Projects.Single().SourceStructure.Module.ShouldEqual("Lending");
    [Fact] void should_report_the_skipped_segments() => _result.Provenance.Projects.Single().SourceStructure.NamespaceSegmentsToSkip.ShouldEqual(2);
}
