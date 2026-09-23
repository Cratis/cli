// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_ConceptTemplates.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_ConceptTemplates.when_building;

public class and_the_member_declares_database_choices : given_discovered_templates
{
    ConceptTemplate? _concept;

    void Because() => _concept = ConceptTemplates.Build(
        [Template("cratis", "Cratis.Templates.Web", null, "C#", databases: ["MongoDB", "PostgreSQL"])]).Single();

    [Fact] void should_offer_the_declared_databases() =>
        _concept!.DatabasesFor("csharp").ShouldContainOnly(["MongoDB", "PostgreSQL"]);

    [Fact] void should_have_its_single_database_choice_done() =>
        ConceptTemplates.Build([Template("console", "Cratis.Templates.Console", null, "C#", databases: ["MongoDB"])])
            .Single().DatabasesFor("csharp").ShouldContainOnly(["MongoDB"]);
}
