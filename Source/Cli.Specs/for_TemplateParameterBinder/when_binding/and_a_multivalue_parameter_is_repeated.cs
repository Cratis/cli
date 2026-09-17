// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateParameterBinder.when_binding;

public class and_a_multivalue_parameter_is_repeated : given_a_binder
{
    ParameterBindingResult? _result;

    void Because() => _result = TemplateParameterBinder.Bind(
        ManifestWith(Choice("Platform", multiple: true)),
        ["--Platform", "net10.0", "--Platform", "net8.0"]);

    [Fact] void should_accumulate_with_the_pipe_separator() => _result!.Values["Platform"].ShouldEqual("net10.0|net8.0");
}
