// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_ConceptTemplates.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_ConceptTemplates.when_building;

public class and_finding_by_member_identity : given_discovered_templates
{
    IReadOnlyList<ConceptTemplate>? _concepts;

    void Establish() => _concepts = ConceptTemplates.Build(
    [
        Template("cratis", "Cratis.Templates.Web", "Cratis.Templates.Cratis", "C#", precedence: 1),
        Template("cratis", "Cratis.Templates.Kotlin", "Cratis.Templates.Cratis", "Kotlin")
    ]);

    [Fact] void should_find_the_concept_from_the_short_name() =>
        ConceptTemplates.Find(_concepts!, "cratis").ShouldNotBeNull();

    [Fact] void should_find_the_concept_from_a_member_identity() =>
        ConceptTemplates.Find(_concepts!, "Cratis.Templates.Kotlin").ShouldNotBeNull();

    [Fact] void should_not_find_unknown_concepts() =>
        ConceptTemplates.Find(_concepts!, "no-such").ShouldBeNull();
}
