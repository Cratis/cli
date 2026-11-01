// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_EnvironmentName;

public class when_validating_names : Specification
{
    [Theory]
    [InlineData("development")]
    [InlineData("production")]
    [InlineData("customer-x")]
    [InlineData("staging.eu_1")]
    void should_accept_safe_names(string name) => EnvironmentName.IsValid(name).ShouldBeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("../secrets")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("-leading")]
    [InlineData("has space")]
    [InlineData("dots..dots")]
    void should_reject_unsafe_names(string name) => EnvironmentName.IsValid(name).ShouldBeFalse();

    [Fact] void should_reject_null() => EnvironmentName.IsValid(null).ShouldBeFalse();
    [Fact] void should_reject_overlong_names() => EnvironmentName.IsValid(new string('a', 65)).ShouldBeFalse();
    [Fact] void should_default_to_development() => EnvironmentName.Default.ShouldEqual("development");
}
