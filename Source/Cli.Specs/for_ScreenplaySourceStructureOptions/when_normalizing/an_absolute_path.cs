// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplaySourceStructureOptions.when_normalizing;

public class an_absolute_path : Specification
{
    const string RawValue = "/private/checkout/Features";

    bool _isValid;
    ScreenplayDiagnostic _diagnostic;

    void Because()
    {
        _isValid = ScreenplaySourceStructureOptions.TryNormalize(
            ScreenplayGenerationOptions.Default with { FeatureRoot = RawValue },
            out _);
        _diagnostic = ScreenplaySourceStructureOptions.InvalidFeatureRoot("/private/checkout/Application.csproj");
    }

    [Fact] void should_be_invalid() => _isValid.ShouldBeFalse();
    [Fact] void should_use_the_shared_invalid_path_code() => _diagnostic.Code.ShouldEqual("DOTNETSP0002");
    [Fact] void should_be_an_error() => _diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_not_echo_the_raw_value() => _diagnostic.Message.ShouldNotContain(RawValue);
    [Fact] void should_use_the_logical_target_identity() => _diagnostic.Location.ShouldEqual("Application.csproj");
}
