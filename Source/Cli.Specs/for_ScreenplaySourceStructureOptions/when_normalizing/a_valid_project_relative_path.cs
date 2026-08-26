// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplaySourceStructureOptions.when_normalizing;

public class a_valid_project_relative_path : Specification
{
    bool _isValid;
    ScreenplayGenerationOptions _result;

    void Because() => _isValid = ScreenplaySourceStructureOptions.TryNormalize(
        ScreenplayGenerationOptions.Default with { FeatureRoot = @"Features\.\Lending//Accounts" },
        out _result);

    [Fact] void should_be_valid() => _isValid.ShouldBeTrue();
    [Fact] void should_normalize_without_changing_the_semantic_path() => _result.FeatureRoot.ShouldEqual("Features/Lending/Accounts");
}
