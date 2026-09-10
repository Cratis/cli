// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class and_feature_root_is_traversing : given.a_marten_application_built_from_source
{
    const string RawFeatureRoot = "../Features";

    GeneratedScreenplay _result;

    void Establish() => Loaded = Loaded with { ProjectSources = [ProjectSource] };

    void Because() => _result = CritterStackScreenplayGeneration.GenerateFrom(
        Loaded,
        "/workspace/Banking/Banking.csproj",
        ScreenplayGenerationOptions.Default with
        {
            Provider = ScreenplayProviders.Marten,
            FeatureRoot = RawFeatureRoot
        });

    [Fact] void should_report_only_the_malformed_root_error() => _result.Diagnostics.Select(_ => _.Code).ShouldContainOnly("DOTNETSP0002");
    [Fact] void should_report_a_safe_generic_message() => _result.Diagnostics.Single().Message.ShouldEqual("The project-relative feature root is invalid");
    [Fact] void should_not_echo_the_raw_root() => _result.Diagnostics.Single().Message.ShouldNotContain(RawFeatureRoot);
    [Fact] void should_use_the_logical_target_identity() => _result.Diagnostics.Single().Location.ShouldEqual("Banking.csproj");
    [Fact] void should_generate_no_source() => _result.Source.ShouldBeEmpty();
    [Fact] void should_not_publish_provenance() => _result.Provenance.ShouldBeNull();
}
