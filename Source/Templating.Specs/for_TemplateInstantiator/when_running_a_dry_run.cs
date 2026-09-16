// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using System.Text;
using Cratis.Templating.Specs.for_TemplateInstantiator.given;

namespace Cratis.Templating.Specs.for_TemplateInstantiator;

public class when_running_a_dry_run : an_instantiator
{
    string? _output;

    void Because()
    {
        var root = CreateTemplate(r => File.WriteAllText(Path.Combine(r, "file.txt"), "TARGET_FRAMEWORK"));
        _output = Path.Combine(Path.GetTempPath(), "cratis-templating-specs", Guid.NewGuid().ToString("N"));
        new TemplateInstantiator().Instantiate(
            TemplateConfigParser.ParseFile(Path.Combine(root, ".template.config", "template.json")),
            root,
            new InstantiationInputs("MyApp", _output, new Dictionary<string, string>(), DryRun: true));
    }

    [Fact] void should_write_nothing() => Directory.Exists(_output).ShouldBeFalse();
}
