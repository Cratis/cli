// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Cli.Commands.Build.for_StudioAddress;

public class when_studio_is_left_out : Specification
{
    StudioAddress _result;

    void Because() => _result = StudioAddress.From(new FlagValue<string>(), "Cratis");

    [Fact] void should_resolve_the_studio() => _result.Url.ShouldEqual(null);
    [Fact] void should_resolve_the_organization() => _result.Organization.ShouldEqual("Cratis");
}
