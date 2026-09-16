// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public class and_a_baseline_is_selected : given_a_template_directory
{
    InstantiationResult? _fromBaseline;
    InstantiationResult? _fromUserValue;

    void Establish()
    {
        Manifest = new TemplateConfig
        {
            Name = "Spec",
            ShortName = "spec",
            Symbols = new Dictionary<string, SymbolConfig>
            {
                ["Framework"] = new()
                {
                    Name = "Framework",
                    Type = SymbolType.Parameter,
                    DataType = "choice",
                    DefaultValue = "net10.0",
                    Choices = [new() { Choice = "net10.0" }, new() { Choice = "net8.0" }],
                    Replaces = "TARGET_FRAMEWORK"
                }
            },
            Baselines = new Dictionary<string, BaselineConfig>
            {
                ["legacy"] = new() { Name = "legacy", Symbols = new Dictionary<string, string> { ["Framework"] = "net8.0" } }
            }
        };
        File.WriteAllText(Path.Combine(Root, "file.txt"), "TARGET_FRAMEWORK");
        WriteManifest();
    }

    void Because()
    {
        _fromBaseline = Instantiate(new InstantiationInputs("App", Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string>(), Baseline: "legacy"));
        _fromUserValue = Instantiate(new InstantiationInputs(
            "App",
            Path.Combine(Root, "..", Guid.NewGuid().ToString("N")),
            new Dictionary<string, string> { ["Framework"] = "net10.0" },
            Baseline: "legacy"));
    }

    [Fact] void should_apply_the_baseline_default() => _fromBaseline!.SymbolValues["Framework"].ShouldEqual("net8.0");

    [Fact] void should_render_the_baseline_value_into_the_content() =>
        File.ReadAllText(Path.Combine(_fromBaseline!.OutputRoot, "file.txt")).ShouldContain("net8.0");

    [Fact] void should_let_a_user_value_win_over_the_baseline() => _fromUserValue!.SymbolValues["Framework"].ShouldEqual("net10.0");
}
