// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_EnvironmentYaml;

public class when_parsing_an_invalid_config_mapping : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => EnvironmentYaml.Parse("config:\n  password: {unknown: value}"));

    [Fact] void should_reject_the_mapping() => _exception.ShouldBeOfExactType<EnvironmentException>();
    [Fact] void should_name_the_key_and_supported_shapes() => _exception.Message.ShouldEqual("Config key 'password' must be a value, 'secure: <base64>' or 'ref: <name>'.");
}
