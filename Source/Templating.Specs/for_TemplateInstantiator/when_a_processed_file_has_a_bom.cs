// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using System.Text;
using Cratis.Templating.Specs.for_TemplateInstantiator.given;

namespace Cratis.Templating.Specs.for_TemplateInstantiator;

public class when_a_processed_file_has_a_bom : an_instantiator
{
    string? _output;

    void Because()
    {
        var root = CreateTemplate(r =>
        {
            var encoding = new UTF8Encoding(true);
            File.WriteAllBytes(Path.Combine(r, "Program.cs"),
                [.. encoding.GetPreamble(), .. encoding.GetBytes("TARGET_FRAMEWORK")]);
        });
        _output = Path.Combine(Path.GetTempPath(), "cratis-templating-specs", Guid.NewGuid().ToString("N"));
        new TemplateInstantiator().Instantiate(
            TemplateConfigParser.ParseFile(Path.Combine(root, ".template.config", "template.json")),
            root,
            new InstantiationInputs("MyApp", _output, new Dictionary<string, string>()));
    }

    [Fact] void should_preserve_the_bom()
    {
        var bytes = File.ReadAllBytes(Path.Combine(_output!, "Program.cs"));
        bytes.ShouldNotBeNull();
        (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF).ShouldBeTrue();
    }

    [Fact] void should_still_replace_tokens() =>
        File.ReadAllText(Path.Combine(_output!, "Program.cs")).ShouldContain("net10.0");
}
