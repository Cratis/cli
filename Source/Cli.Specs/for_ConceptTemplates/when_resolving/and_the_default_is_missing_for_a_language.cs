// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_ConceptTemplates.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_ConceptTemplates.when_resolving;

public class and_the_default_is_missing_for_a_language : given_discovered_templates
{
    IReadOnlyList<ConceptTemplate>? _concepts;

    void Establish() => _concepts = ConceptTemplates.Build(
    [
        Standalone("cratis", "C#"),
        Standalone("kotlin-console", "Kotlin"),
        Standalone("java-console", "Java")
    ]);

    [Fact] void should_not_substitute_the_csharp_default_for_kotlin() =>
        ConceptTemplates.FindForLanguage(_concepts!, LanguageSelection.Resolve("kotlin").DefaultTemplate, "kotlin").ShouldBeNull();

    [Fact] void should_offer_the_kotlin_templates_in_the_error() =>
        ConceptTemplates.AvailableForLanguage(_concepts!, "kotlin").ShouldContainOnly(["kotlin-console"]);

    [Fact] void should_not_list_templates_of_other_languages_in_the_error() =>
        ConceptTemplates.AvailableForLanguage(_concepts!, "java").ShouldContainOnly(["java-console"]);
}
