// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public class and_an_unknown_baseline_is_selected : given_a_template_directory
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => Instantiate(new InstantiationInputs("App", Path.Combine(Root, "..", Guid.NewGuid().ToString("N")), new Dictionary<string, string>(), Baseline: "no-such")));

    [Fact] void should_fail_naming_the_known_baselines() => _error!.Message.ShouldContain("no-such");
}
