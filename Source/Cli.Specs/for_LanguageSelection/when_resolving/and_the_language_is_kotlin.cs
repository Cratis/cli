// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_LanguageSelection.when_resolving;

public class and_the_language_is_kotlin : Specification
{
    LanguageSelectionResult? _result;

    void Because() => _result = LanguageSelection.Resolve("kotlin");

    [Fact] void should_resolve_the_kotlin_package() => _result!.PackageId.ShouldEqual("Cratis.Templates.Kotlin");

    [Fact] void should_default_to_the_cratis_kotlin_template() => _result!.DefaultTemplate.ShouldEqual("cratis-kotlin");

    [Fact] void should_have_no_errors() => _result!.Errors.ShouldBeEmpty();
}
