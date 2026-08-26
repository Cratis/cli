// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ArcScreenplayGeneration.when_generating;

public class and_feature_root_is_requested : given.an_application_built_from_source
{
    const string RawFeatureRoot = "/private/checkout/Features";

    GeneratedScreenplay _baseline;
    GeneratedScreenplay _result;

    void Because()
    {
        _baseline = ArcScreenplayGeneration.GenerateFrom(Loaded, $"{ProjectName}.csproj", ScreenplayGenerationOptions.Default);
        _result = ArcScreenplayGeneration.GenerateFrom(
            Loaded,
            $"/private/checkout/{ProjectName}.csproj",
            ScreenplayGenerationOptions.Default with { FeatureRoot = RawFeatureRoot });
    }

    [Fact] void should_report_that_the_option_was_not_applied() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnsupportedGenerationOption);
    [Fact] void should_not_echo_the_raw_option() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnsupportedGenerationOption).Message.ShouldNotContain(RawFeatureRoot);
    [Fact] void should_use_the_logical_target_identity() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnsupportedGenerationOption).Location.ShouldEqual($"{ProjectName}.csproj");
    [Fact] void should_preserve_the_legacy_document_bytes() => _result.Source.ShouldEqual(_baseline.Source);
}
