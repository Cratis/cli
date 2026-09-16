// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Remove unused private members - methods used by Specification framework via reflection
#pragma warning disable RCS1213 // Remove unused method declaration - methods used by Specification framework via reflection

using Cratis.Templating.Conformance.given;

namespace Cratis.Templating.Conformance;

public class when_rendering_value_forms : a_conformance_spec
{
    InstantiationResult? _result;

    void Because() => _result = Instantiate("ConformanceValueForms", "My-App");

    [Fact]
    void should_replace_the_namespace_form() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "Program.cs")).ShouldContain("namespace My_App;");

    [Fact]
    void should_replace_the_class_name_form() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "Program.cs")).ShouldContain("public class My_App");

    [Fact]
    void should_replace_the_identity_form() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "Program.cs")).ShouldContain("\"My-App\"");

    [Fact]
    void should_rename_directories_using_the_namespace_form() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "My_App", "nested.txt")).ShouldContain("My_App");

    [Fact]
    void should_replace_every_form_in_the_nested_file() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, "My_App", "nested.txt")).ShouldNotContain("Template");
}
