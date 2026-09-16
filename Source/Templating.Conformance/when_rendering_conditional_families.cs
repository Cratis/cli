// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Remove unused private members - methods used by Specification framework via reflection
#pragma warning disable RCS1213 // Remove unused method declaration - methods used by Specification framework via reflection

using Cratis.Templating.Conformance.given;

namespace Cratis.Templating.Conformance;

public class when_rendering_conditional_families : a_conformance_spec
{
    InstantiationResult? _modern;
    InstantiationResult? _legacy;

    void Because()
    {
        _modern = Instantiate("ConformanceConditional", "MyApp");
        _legacy = Instantiate("ConformanceConditional", "MyApp", new Dictionary<string, string>
        {
            ["Framework"] = "net8.0",
            ["UseAnalytics"] = "true"
        });
    }

    [Fact]
    void should_evaluate_the_if_branch_in_language_files() =>
        File.ReadAllText(Path.Combine(_modern!.OutputRoot, "Program.cs")).ShouldContain("modern framework");

    [Fact]
    void should_evaluate_the_else_branch_in_language_files() =>
        File.ReadAllText(Path.Combine(_legacy!.OutputRoot, "Program.cs")).ShouldContain("legacy framework");

    [Fact]
    void should_uncomment_the_actionable_branch_in_json_files() =>
        File.ReadAllText(Path.Combine(_legacy!.OutputRoot, "appsettings.json")).ShouldContain("\"analytics\": true");

    [Fact]
    void should_keep_the_inactive_branch_out_in_json_files() =>
        File.ReadAllText(Path.Combine(_legacy!.OutputRoot, "appsettings.json")).ShouldNotContain("\"analytics\": false");

    [Fact]
    void should_include_the_block_when_true_in_xml_files() =>
        File.ReadAllText(Path.Combine(_legacy!.OutputRoot, "layout.xml")).ShouldContain("<analytics enabled=\"true\" />");

    [Fact]
    void should_include_the_block_when_true_in_single_hash_files() =>
        File.ReadAllText(Path.Combine(_legacy!.OutputRoot, "deploy.yml")).ShouldContain("analytics");

    [Fact]
    void should_remove_directives_everywhere() =>
        File.ReadAllText(Path.Combine(_legacy!.OutputRoot, "deploy.yml")).ShouldNotContain("#if");
}
