// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using System.Text;
using Cratis.Templating.Specs.for_TemplateInstantiator.given;

namespace Cratis.Templating.Specs.for_TemplateInstantiator;

public class when_the_output_directory_is_not_empty : an_instantiator
{
    string? _output;
    Exception? _error;

    void Because()
    {
        var root = CreateTemplate(r => File.WriteAllText(Path.Combine(r, "file.txt"), "x"));
        _output = Path.Combine(Path.GetTempPath(), "cratis-templating-specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_output);
        File.WriteAllText(Path.Combine(_output, "existing.txt"), "do not touch");
        _error = Catch.Exception(() => new TemplateInstantiator().Instantiate(
            TemplateConfigParser.ParseFile(Path.Combine(root, ".template.config", "template.json")),
            root,
            new InstantiationInputs("MyApp", _output, new Dictionary<string, string>())));
    }

    [Fact] void should_refuse_without_force() => _error.ShouldNotBeNull();

    [Fact] void should_not_touch_existing_files() => File.ReadAllText(Path.Combine(_output!, "existing.txt")).ShouldEqual("do not touch");
}
