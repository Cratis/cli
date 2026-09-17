// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using System.Text;
using Cratis.Templating.Specs.for_TemplateInstantiator.given;

namespace Cratis.Templating.Specs.for_TemplateInstantiator;

public class when_a_file_is_copy_only : an_instantiator
{
    string? _output;
    string? _source;

    void Because()
    {
        const string manifest = "{ \"name\": \"Spec Template\", \"shortName\": \"spec\", \"symbols\": { \"Framework\": { \"type\": \"parameter\", \"datatype\": \"choice\", \"choices\": [ { \"choice\": \"net10.0\" } ], \"defaultValue\": \"net10.0\", \"replaces\": \"TARGET_FRAMEWORK\" } }, \"sources\": [ { \"copyOnly\": [\"*.bin\"], \"exclude\": [\"**/.template.config/**\"] } ] }";
        _source = CreateTemplate(root => File.WriteAllBytes(Path.Combine(root, "asset.bin"), [0x00, 0xFF, 0x10, 0x20]), manifest);
        _output = Path.Combine(Path.GetTempPath(), "cratis-templating-specs", Guid.NewGuid().ToString("N"));
        new TemplateInstantiator().Instantiate(
            TemplateConfigParser.ParseFile(Path.Combine(_source, ".template.config", "template.json")),
            _source,
            new InstantiationInputs("MyApp", _output, new Dictionary<string, string>()));
    }

    [Fact] void should_copy_the_file_byte_exact() =>
        File.ReadAllBytes(Path.Combine(_output!, "asset.bin")).ShouldEqual([0x00, 0xFF, 0x10, 0x20]);
}
