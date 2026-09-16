// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateParameterBinder.when_binding;

public class and_a_boolean_switch_is_passed_without_a_value : given_a_binder
{
    ParameterBindingResult? _result;

    void Because() => _result = TemplateParameterBinder.Bind(
        ManifestWith(Bool("IncludeTests")),
        ["--IncludeTests"]);

    [Fact] void should_default_to_true() => _result!.Values["IncludeTests"].ShouldEqual("true");
}
