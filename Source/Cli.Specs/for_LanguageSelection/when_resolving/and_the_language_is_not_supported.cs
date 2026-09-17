// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_LanguageSelection.when_resolving;

public class and_the_language_is_not_supported : Specification
{
    LanguageSelectionResult? _result;

    void Because() => _result = LanguageSelection.Resolve("fsharp");

    [Fact] void should_error_listing_the_supported_languages() =>
        _result!.Errors[0].ShouldContain("csharp, kotlin, java");
}
