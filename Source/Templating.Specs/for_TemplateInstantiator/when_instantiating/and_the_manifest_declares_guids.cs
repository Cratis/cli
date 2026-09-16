// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public class and_the_manifest_declares_guids : given_a_template_directory
{
    InstantiationResult? _result;

    void Establish()
    {
        const string stable = "8e23b1e0-d6b3-4f1b-9db2-c55d49a2b311";
        Manifest = new TemplateConfig { Name = "Spec", ShortName = "spec", Guids = [stable] };
        File.WriteAllText(
            Path.Combine(Root, "guids.txt"),
            $"lower: {stable}\nUPPER: {stable.ToUpperInvariant()}\nuntouched: 12345678-1234-1234-1234-123456789012");
    }

    void Because() => _result = Instantiate(new InstantiationInputs("App", Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string>()));

    [Fact] void should_replace_every_configured_guid_occurrence() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "guids.txt")).ShouldNotContain("8e23b1e0");

    [Fact] void should_pass_unrelated_guids_through_untouched() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "guids.txt")).ShouldContain("12345678-1234-1234-1234-123456789012");

    [Fact] void should_produce_a_valid_guid_in_place_of_the_lowercase_form() =>
        Guid.TryParseExact(
            File.ReadAllText(Path.Combine(_result!.OutputRoot, "guids.txt")).Split("lower: ")[1].Split('\n')[0],
            "D",
            out _).ShouldBeTrue();
}
