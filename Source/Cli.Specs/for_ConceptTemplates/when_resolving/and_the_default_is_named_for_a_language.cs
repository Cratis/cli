// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_ConceptTemplates.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_ConceptTemplates.when_resolving;

public class and_the_default_is_named_for_a_language : given_discovered_templates
{
    IReadOnlyList<ConceptTemplate>? _concepts;

    void Establish() => _concepts = ConceptTemplates.Build(
    [
        Template("cratis", "Cratis.Templates.Web", "Cratis.Templates.Cratis", "C#", precedence: 1),
        Template("cratis", "Cratis.Templates.Kotlin", "Cratis.Templates.Cratis", "Kotlin"),
        Template("cratis", "Cratis.Templates.Java", "Cratis.Templates.Cratis", "Java"),
        Standalone("cratis-aspire")
    ]);

    [Fact] void should_resolve_the_kotlin_default_from_its_language_selection() =>
        ConceptTemplates.FindForLanguage(_concepts!, LanguageSelection.Resolve("kotlin").DefaultTemplate, "kotlin")!
            .Manifest.Identity.ShouldEqual("Cratis.Templates.Kotlin");

    [Fact] void should_resolve_the_java_default_from_its_language_selection() =>
        ConceptTemplates.FindForLanguage(_concepts!, LanguageSelection.Resolve("java").DefaultTemplate, "java")!
            .Manifest.Identity.ShouldEqual("Cratis.Templates.Java");

    [Fact] void should_not_offer_a_csharp_only_template_for_kotlin() =>
        ConceptTemplates.FindForLanguage(_concepts!, "cratis-aspire", "kotlin").ShouldBeNull();

    [Fact] void should_list_only_the_templates_available_for_kotlin() =>
        ConceptTemplates.AvailableForLanguage(_concepts!, "kotlin").ShouldContainOnly(["cratis"]);
}
