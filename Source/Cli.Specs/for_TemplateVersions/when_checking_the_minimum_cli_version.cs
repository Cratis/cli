// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateVersions;

public class when_checking_the_minimum_cli_version : Specification
{
    [Fact] void should_accept_a_cli_at_the_minimum() => TemplateVersions.IsSatisfiedBy("3.0.0", "3.0.0").ShouldBeTrue();
    [Fact] void should_accept_a_newer_cli() => TemplateVersions.IsSatisfiedBy("3.10.0", "3.9.1").ShouldBeTrue();
    [Fact] void should_refuse_an_older_cli() => TemplateVersions.IsSatisfiedBy("2.9.0", "3.0.0").ShouldBeFalse();
    [Fact] void should_accept_any_cli_without_a_minimum() => TemplateVersions.IsSatisfiedBy("2.9.0", null).ShouldBeTrue();
    [Fact] void should_accept_a_development_build() => TemplateVersions.IsSatisfiedBy(TemplateVersions.DevelopmentCliVersion, "3.0.0").ShouldBeTrue();
    [Fact] void should_order_by_the_release_part() => TemplateVersions.Latest(["1.7.4", "1.10.0", "1.9.0-beta.1", "nightly"]).ShouldEqual("1.10.0");
}
