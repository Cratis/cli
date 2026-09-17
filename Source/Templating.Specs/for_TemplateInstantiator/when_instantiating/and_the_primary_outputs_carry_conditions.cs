// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public class and_the_primary_outputs_carry_conditions : given_a_template_directory
{
    InstantiationResult? _both;
    InstantiationResult? _one;

    void Establish()
    {
        Manifest = new TemplateConfig
        {
            Name = "Spec",
            ShortName = "spec",
            Symbols = new Dictionary<string, SymbolConfig>
            {
                ["Modern"] = new() { Name = "Modern", Type = SymbolType.Parameter, DataType = "bool", DefaultValue = "true" }
            },
            PrimaryOutputs =
            [
                new PrimaryOutputConfig { Path = "always.txt" },
                new PrimaryOutputConfig { Path = "modern.txt", Condition = "Modern == \"true\"" }
            ]
        };
        File.WriteAllText(Path.Combine(Root, "always.txt"), "x");
        File.WriteAllText(Path.Combine(Root, "modern.txt"), "y");
    }

    void Because()
    {
        _both = Instantiate(new InstantiationInputs("App", Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string>()));
        _one = Instantiate(new InstantiationInputs("App", Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string> { ["Modern"] = "false" }));
    }

    [Fact] void should_always_list_unconditional_outputs() => _both!.PrimaryOutputs.Count.ShouldEqual(2);

    [Fact] void should_drop_conditional_outputs_when_the_condition_is_false() => _one!.PrimaryOutputs.Count.ShouldEqual(1);

    [Fact] void should_drop_the_conditioned_output() =>
        _one!.PrimaryOutputs[0].ShouldContain("always.txt");
}
