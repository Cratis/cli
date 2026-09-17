// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_ConceptTemplates.given;
using Cratis.Cli.Templates;
using Cratis.Templating;
using Cratis.Templating.Configuration;
using Cratis.Templating.Packages;

namespace Cratis.Cli.for_ConceptTemplates.when_building;

public class and_the_member_has_no_database_parameter : given_discovered_templates
{
    ConceptTemplate? _concept;

    void Because() => _concept = ConceptTemplates.Build(
    [
        new DiscoveredTemplate(
            new TemplateConfig
            {
                Name = "bare",
                ShortName = "bare",
                Identity = "Cratis.Templates.Bare",
                Tags = new Dictionary<string, string> { ["language"] = "C#" }
            },
            "/templates/bare")
    ]).Single();

    [Fact] void should_offer_no_databases_so_the_choice_is_decided() =>
        _concept!.DatabasesFor("csharp").ShouldBeEmpty();
}
