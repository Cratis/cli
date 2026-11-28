// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_StudioAddress;

public class when_a_copied_builder_address_contains_a_query : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => StudioAddress.From(new FlagValue<string> { IsSet = true, Value = "https://studio.example/builders/connect/acme?private" }, null));

    [Fact] void should_refuse_it_before_extracting_the_studio() => _error.ShouldBeOfExactType<StudioNotConnected>();
}
