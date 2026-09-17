// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_ConceptTemplates.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_ConceptTemplates.when_building;

public class and_language_derivatives_share_a_group_identity : given_discovered_templates
{
    IReadOnlyList<ConceptTemplate>? _concepts;

    void Because() => _concepts = ConceptTemplates.Build(
    [
        Template("cratis", "Cratis.Templates.Web", "Cratis.Templates.Cratis", "C#", precedence: 1),
        Template("cratis", "Cratis.Templates.Kotlin", "Cratis.Templates.Cratis", "Kotlin"),
        Template("cratis", "Cratis.Templates.Java", "Cratis.Templates.Cratis", "Java"),
        Standalone("cratis-aspire")
    ]);

    [Fact] void should_collapse_the_derivatives_into_one_concept() =>
        _concepts!.Select(concept => concept.ShortName).ShouldContainOnly(["cratis", "cratis-aspire"]);

    [Fact] void should_offer_every_language_of_the_group() =>
        _concepts!.Single(concept => concept.ShortName == "cratis").Languages.ShouldContainOnly(["csharp", "java", "kotlin"]);

    [Fact] void should_take_the_highest_precedence_member_as_the_default() =>
        _concepts!.Single(concept => concept.ShortName == "cratis").DefaultLanguage.ShouldEqual("csharp");

    [Fact] void should_resolve_each_member_by_language()
    {
        var concept = _concepts!.Single(concept => concept.ShortName == "cratis");
        concept.MemberFor("csharp")!.Manifest.Identity.ShouldEqual("Cratis.Templates.Web");
        concept.MemberFor("kotlin")!.Manifest.Identity.ShouldEqual("Cratis.Templates.Kotlin");
        concept.MemberFor("java")!.Manifest.Identity.ShouldEqual("Cratis.Templates.Java");
    }
}
