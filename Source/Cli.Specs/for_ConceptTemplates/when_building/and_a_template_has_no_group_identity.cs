// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_ConceptTemplates.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_ConceptTemplates.when_building;

public class and_a_template_has_no_group_identity : given_discovered_templates
{
    IReadOnlyList<ConceptTemplate>? _concepts;

    void Because() => _concepts = ConceptTemplates.Build([Standalone("cratis-aspire")]);

    [Fact] void should_be_its_own_concept() => _concepts!.Single().ShortName.ShouldEqual("cratis-aspire");

    [Fact] void should_offer_one_language() => _concepts!.Single().Languages.ShouldContainOnly(["csharp"]);

    [Fact] void should_default_to_csharp() => _concepts!.Single().DefaultLanguage.ShouldEqual("csharp");
}
