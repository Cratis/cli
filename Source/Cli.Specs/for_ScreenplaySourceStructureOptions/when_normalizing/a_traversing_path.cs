// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplaySourceStructureOptions.when_normalizing;

public class a_traversing_path : Specification
{
    bool _isValid;

    void Because() => _isValid = ScreenplaySourceStructureOptions.TryNormalize(
        ScreenplayGenerationOptions.Default with { FeatureRoot = "Features/../Secrets" },
        out _);

    [Fact] void should_be_invalid_instead_of_being_sanitized() => _isValid.ShouldBeFalse();
}
