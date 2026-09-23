// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateCatalogue;

public class when_reading_the_language_table : Specification
{
    [Fact] void should_carry_the_three_languages() =>
        TemplateCatalogue.Languages.Select(language => language.Language).ShouldContainOnly(["csharp", "kotlin", "java"]);

    [Fact] void should_publish_csharp_with_the_cratis_template() =>
        TemplateCatalogue.FindLanguage("csharp")!.DefaultTemplate.ShouldEqual("cratis");

    [Fact] void should_point_kotlin_at_the_future_template() =>
        TemplateCatalogue.FindLanguage("kotlin")!.DefaultTemplate.ShouldEqual("cratis-kotlin");

    [Fact] void should_point_java_at_the_future_template() =>
        TemplateCatalogue.FindLanguage("java")!.DefaultTemplate.ShouldEqual("cratis-java");

    [Fact] void should_publish_every_language() =>
        TemplateCatalogue.Languages.Where(language => language.IsPublished)
            .Select(language => language.Language).ShouldContainOnly(["csharp", "kotlin", "java"]);

    [Fact] void should_pin_all_three_packages_in_lockstep() =>
        TemplateCatalogue.PinnedPackages.ShouldContainOnly(
            ["Cratis.Templates", "Cratis.Templates.Kotlin", "Cratis.Templates.Java"]);

    [Fact] void should_look_languages_up_case_insensitively() =>
        TemplateCatalogue.FindLanguage("CSHARP").ShouldNotBeNull();
}
