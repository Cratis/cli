// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;
using Spectre.Console.Cli;

namespace Cratis.Cli.for_NewSettings;

public class when_validating_the_language : Specification
{
    [Fact] void should_require_the_language_when_a_template_is_named() =>
        new NewSettings { Template = "cratis" }.Validate().Successful.ShouldBeFalse();

    [Fact] void should_accept_the_supported_languages_case_insensitively() =>
        new[] { "csharp", "CSharp", "kotlin", "KOTLIN", "java", "Java" }
            .All(language => new NewSettings { Language = language, Template = "cratis" }.Validate().Successful)
            .ShouldBeTrue();

    [Fact] void should_accept_the_hash_alias() =>
        new NewSettings { Language = "C#", Template = "cratis" }.Validate().Successful.ShouldBeTrue();

    [Fact] void should_reject_an_unsupported_language() =>
        new NewSettings { Language = "fsharp", Template = "cratis" }.Validate().Successful.ShouldBeFalse();

    [Fact] void should_accept_listing_without_a_language() =>
        new NewSettings().Validate().Successful.ShouldBeTrue();
}
