// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_LanguageSelection.when_resolving;

public class and_the_language_is_java : Specification
{
    LanguageSelectionResult? _result;

    void Because() => _result = LanguageSelection.Resolve("java");

    [Fact] void should_carry_the_future_default_template() => _result!.DefaultTemplate.ShouldEqual("cratis-java");

    [Fact] void should_error_that_the_package_is_not_published_yet() =>
        _result!.Errors[0].ShouldContain("no template package for language 'java' is published yet");
}
