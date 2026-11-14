// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Cli.Commands.Build.for_StudioAddress;

public class when_studio_is_given_without_an_address : Specification
{
    StudioAddress _result;

    void Because() => _result = StudioAddress.From(new FlagValue<string> { IsSet = true }, "Cratis");

    [Fact] void should_resolve_the_studio() => _result.Url.ShouldEqual(StudioAddress.Hosted);
    [Fact] void should_resolve_the_organization() => _result.Organization.ShouldEqual("Cratis");
}
