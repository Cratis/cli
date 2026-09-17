// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public class and_the_placeholder_filename_convention_applies : given_a_template_directory
{
    InstantiationResult? _result;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(Root, "empty-dir-placeholders"));
        File.WriteAllText(Path.Combine(Root, "empty-dir-placeholders", "_"), "marker");
        File.WriteAllText(Path.Combine(Root, "_.gitignore"), "ignored\n");
    }

    void Because() => _result = Instantiate(new InstantiationInputs("App", Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string>()));

    [Fact] void should_create_an_empty_directory_for_a_file_named_as_the_placeholder() =>
        File.Exists(Path.Combine(_result!.OutputRoot, "empty-dir-placeholders", "_")).ShouldBeFalse();

    [Fact] void should_strip_a_leading_placeholder_from_file_names() =>
        File.ReadAllText(Path.Combine(_result!.OutputRoot, ".gitignore")).ShouldEqual("ignored\n");
}
