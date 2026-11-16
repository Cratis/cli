// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using YamlDotNet.Core;

namespace Cratis.Environments.for_EnvironmentYaml;

public class when_parsing_invalid_yaml : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => EnvironmentYaml.Parse("config: [unterminated"));

    [Fact] void should_wrap_the_yaml_error() => _exception.ShouldBeOfExactType<EnvironmentException>();
    [Fact] void should_retain_the_yaml_cause() => (_exception.InnerException is YamlException).ShouldBeTrue();
    [Fact] void should_include_the_yaml_diagnostic() => _exception.Message.ShouldEqual($"The environment file is not valid YAML: {_exception.InnerException!.Message}");
}
