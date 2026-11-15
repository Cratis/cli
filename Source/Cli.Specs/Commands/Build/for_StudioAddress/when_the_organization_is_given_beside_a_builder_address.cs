// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Cli.Commands.Build.for_StudioAddress;

public class when_the_organization_is_given_beside_a_builder_address : Specification
{
    StudioAddress _result;

    void Because() => _result = StudioAddress.From(new FlagValue<string> { IsSet = true, Value = "https://app.cratis.studio/builders/connect/Cratis" }, "acme");

    [Fact] void should_resolve_the_studio() => _result.Url.ShouldEqual("https://app.cratis.studio");
    [Fact] void should_resolve_the_organization() => _result.Organization.ShouldEqual("acme");
}
