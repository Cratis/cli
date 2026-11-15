// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public class and_a_primary_output_leaves_the_output : given_a_template_directory
{
    Exception? _error;

    void Establish() => Manifest = new TemplateConfig
    {
        Name = "Spec",
        ShortName = "spec",
        PrimaryOutputs = [new PrimaryOutputConfig { Path = Path.Combine("..", "victim.csproj") }]
    };

    void Because() => _error = Catch.Exception(() => Instantiate(new InstantiationInputs("App", Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string>())));

    [Fact] void should_refuse_the_manifest() => _error.ShouldBeOfExactType<InvalidTemplateManifest>();
}
