// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_ConceptTemplates.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_ConceptTemplates.when_resolving;

public class and_a_standalone_template_shares_a_concept_short_name : given_discovered_templates
{
    IReadOnlyList<ConceptTemplate>? _concepts;

    void Establish() => _concepts = ConceptTemplates.Build(
    [
        Template("cratis", "Cratis.Templates.Web", "Cratis.Templates.Cratis", "C#", precedence: 1),
        Template("cratis", "Custom.Templates.Kotlin", null, "Kotlin")
    ]);

    [Fact] void should_resolve_the_standalone_kotlin_template_by_short_name() =>
        ConceptTemplates.FindForLanguage(_concepts!, "cratis", "kotlin")!
            .Manifest.Identity.ShouldEqual("Custom.Templates.Kotlin");

    [Fact] void should_resolve_the_csharp_concept_by_short_name_for_csharp() =>
        ConceptTemplates.FindForLanguage(_concepts!, "cratis", "csharp")!
            .Manifest.Identity.ShouldEqual("Cratis.Templates.Web");

    [Fact] void should_not_resolve_the_short_name_for_an_unavailable_language() =>
        ConceptTemplates.FindForLanguage(_concepts!, "cratis", "java").ShouldBeNull();
}
