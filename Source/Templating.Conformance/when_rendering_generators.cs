// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Remove unused private members - methods used by Specification framework via reflection
#pragma warning disable RCS1213 // Remove unused method declaration - methods used by Specification framework via reflection

using Cratis.Templating.Conformance.given;

namespace Cratis.Templating.Conformance;

public class when_rendering_generators : a_conformance_spec
{
    InstantiationResult? _result;

    void Because() => _result = Instantiate("ConformanceGenerators", "MyApp");

    [Fact]
    void should_evaluate_the_switch_generator_cases() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "README.md")).ShouldContain("yarn install");

    [Fact]
    void should_produce_a_guid_from_the_guid_generator() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "README.md")).ShouldContain("Guid: ");

    [Fact]
    void should_not_leave_the_guid_token() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "README.md")).ShouldNotContain("PROJECT_GUID");

    [Fact]
    void should_produce_a_year_from_the_now_generator() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "README.md")).ShouldContain($"Year: {DateTime.UtcNow.Year}");

    [Fact]
    void should_produce_a_port_in_range_from_the_port_generator()
    {
        var port = File.ReadAllText(Path.Combine(_result!.OutputRoot, "README.md"))
            .Split("Port: ")[1].Split('\n')[0];
        var value = int.Parse(port);
        (value >= 5000 && value <= 9000).ShouldBeTrue();
    }

    [Fact]
    void should_lower_the_parameter_with_the_casing_generator() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "README.md")).ShouldContain("Slug: yarn");

    [Fact]
    void should_apply_the_form_to_the_derived_symbol() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "README.md")).ShouldContain("# Yarn");
}
