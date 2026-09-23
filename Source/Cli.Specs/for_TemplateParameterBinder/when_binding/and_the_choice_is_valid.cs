// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateParameterBinder.when_binding;

public class and_the_choice_is_valid : given_a_binder
{
    ParameterBindingResult? _result;

    void Because() => _result = TemplateParameterBinder.Bind(
        ManifestWith(Choice("Framework")),
        ["--Framework", "net8.0"]);

    [Fact] void should_bind_the_value() => _result!.Values["Framework"].ShouldEqual("net8.0");
    [Fact] void should_have_no_errors() => _result!.Errors.ShouldBeEmpty();
}
