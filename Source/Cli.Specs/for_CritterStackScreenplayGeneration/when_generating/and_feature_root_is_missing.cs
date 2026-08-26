// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class and_feature_root_is_missing : given.a_marten_application_built_from_source
{
    GeneratedScreenplay _result;

    void Establish() => Loaded = Loaded with { ProjectSources = [ProjectSource] };

    void Because() => _result = CritterStackScreenplayGeneration.GenerateFrom(
        Loaded,
        "/workspace/Banking/Banking.csproj",
        ScreenplayGenerationOptions.Default with
        {
            Provider = ScreenplayProviders.Marten,
            FeatureRoot = "Features"
        });

    [Fact] void should_propagate_the_strict_placement_error() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("DOTNETSP0003");
    [Fact] void should_not_fall_back_to_flat_compatibility() => _result.Source.ShouldNotContain("readmodel Account");
}
