// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateParameterBinder.when_binding;

public class and_the_parameter_is_unknown : given_a_binder
{
    ParameterBindingResult? _result;

    void Because() => _result = TemplateParameterBinder.Bind(
        ManifestWith(Choice("Framework")),
        ["--Nope", "value"]);

    [Fact] void should_error_naming_the_valid_set() => _result!.Errors[0].ShouldContain("--Framework");
}
