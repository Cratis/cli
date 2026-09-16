// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateParameterBinder.when_binding;

public class and_the_choice_is_invalid : given_a_binder
{
    ParameterBindingResult? _result;

    void Because() => _result = TemplateParameterBinder.Bind(
        ManifestWith(Choice("Framework")),
        ["--Framework", "net5.0"]);

    [Fact] void should_report_the_error() => _result!.Errors.Count.ShouldEqual(1);
    [Fact] void should_name_the_valid_choices() => _result!.Errors[0].ShouldContain("net10.0, net8.0, net6.0");
}
