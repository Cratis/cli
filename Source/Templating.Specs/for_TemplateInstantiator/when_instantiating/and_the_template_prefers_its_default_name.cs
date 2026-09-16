// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public class and_the_template_prefers_its_default_name : given_a_template_directory
{
    Exception? _error;

    void Establish()
    {
        Manifest = new TemplateConfig { Name = "Spec", ShortName = "spec", DefaultName = "Fixed", PreferDefaultName = true };
    }

    void Because() => _error = Catch.Exception(() => Instantiate(new InstantiationInputs("SomethingElse", Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string>())));

    [Fact] void should_refuse_a_different_name() => _error.ShouldNotBeNull();

    [Fact] void should_use_the_default_name_when_none_is_given() =>
        Instantiate(new InstantiationInputs(null, Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string>())).Name.ShouldEqual("Fixed");
}
