// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using System.Text;
using Cratis.Templating.Specs.for_TemplateInstantiator.given;

namespace Cratis.Templating.Specs.for_TemplateInstantiator;

public class when_instantiating_with_source_name_forms : an_instantiator
{
    InstantiationResult? _result;
    string? _root;
    string? _output;

    void Because()
    {
        _root = CreateTemplate(root =>
        {
            File.WriteAllText(Path.Combine(root, "Program.cs"),
                "namespace Template._1;\n\npublic class Template__1\n{\n    var s = \"Template.1\";\n}\n");
            Directory.CreateDirectory(Path.Combine(root, "Template.1"));
            File.WriteAllText(Path.Combine(root, "Template.1", "nested.cs"), "// Template.1");
        });
        var output = Path.Combine(Path.GetTempPath(), "cratis-templating-specs", Guid.NewGuid().ToString("N"));
        _output = output;
        _result = new TemplateInstantiator().Instantiate(
            TemplateConfigParser.ParseFile(Path.Combine(_root, ".template.config", "template.json")),
            _root,
            new InstantiationInputs("My-App", output, new Dictionary<string, string>()));
    }

    [Fact] void should_replace_the_namespace_form() => File.ReadAllText(Path.Combine(_output!, "Program.cs")).ShouldContain("namespace My_App;");
    [Fact] void should_replace_the_class_name_form() => File.ReadAllText(Path.Combine(_output!, "Program.cs")).ShouldContain("public class My_App");
    [Fact] void should_replace_the_identity_form() => File.ReadAllText(Path.Combine(_output!, "Program.cs")).ShouldContain("\"My-App\"");
    [Fact] void should_rename_directories_named_after_the_source_name() => Directory.Exists(Path.Combine(_output!, "My-App")).ShouldBeTrue();
}
