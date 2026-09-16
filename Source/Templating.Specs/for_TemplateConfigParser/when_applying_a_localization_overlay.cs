// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Specs.for_TemplateConfigParser.given;

namespace Cratis.Templating.Specs.for_TemplateConfigParser;

public class when_applying_a_localization_overlay : a_parser
{
    TemplateConfig? _localized;

    void Because() => _localized = LocalizationStore.Apply(
        Parse("{ \"name\": \"Console\", \"shortName\": \"console\", \"symbols\": { \"Framework\": { \"type\": \"parameter\", \"datatype\": \"choice\", \"choices\": [ { \"choice\": \"net10.0\" } ] } } }"),
        new Dictionary<string, string>
        {
            ["name"] = "Konsole",
            ["description"] = "Eine Vorlage",
            ["symbols/Framework/description"] = "Zielframework",
            ["symbols/Framework/choices/net10.0/description"] = ".NET 10"
        });

    [Fact] void should_localize_the_name() => _localized!.Name.ShouldEqual("Konsole");
    [Fact] void should_localize_the_description() => _localized!.Description.ShouldEqual("Eine Vorlage");
    [Fact] void should_localize_symbol_descriptions() => _localized!.Symbols["Framework"].Description.ShouldEqual("Zielframework");
    [Fact] void should_localize_choice_descriptions() => _localized!.Symbols["Framework"].Choices[0].Description.ShouldEqual(".NET 10");
}
