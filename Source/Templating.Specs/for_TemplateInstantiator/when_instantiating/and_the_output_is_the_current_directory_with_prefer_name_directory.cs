// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public class and_the_output_is_the_current_directory_with_prefer_name_directory : given_a_template_directory
{
    InstantiationResult? _result;

    void Establish()
    {
        Manifest = new TemplateConfig { Name = "Spec", ShortName = "spec", PreferNameDirectory = true };
        File.WriteAllText(Path.Combine(Root, "file.txt"), "x");

        // Explicit output wins over preferNameDirectory: files land in the given directory itself.
    }

    void Because() => _result = Instantiate(new InstantiationInputs("App", Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string>()));

    [Fact] void should_use_an_explicit_output_directory_directly() =>
        File.Exists(Path.Combine(_result!.OutputRoot, "file.txt")).ShouldBeTrue();
}
