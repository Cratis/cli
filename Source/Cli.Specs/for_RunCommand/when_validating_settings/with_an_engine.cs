// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_validating_settings;

public class with_an_engine : Specification
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("eventmodel", "eventmodel")]
    [InlineData("SEMANTIC", "semantic")]
    [InlineData("EventModel", "eventmodel")]
    void should_accept_and_normalize_supported_engines(string? engine, string? expected)
    {
        var settings = new RunSettings { Engine = engine };
        settings.Validate().Successful.ShouldBeTrue();
        settings.Engine.ShouldEqual(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData(" semantic ")]
    void should_list_both_engines_for_invalid_selections(string engine)
    {
        var result = new RunSettings { Engine = engine }.Validate();
        result.Successful.ShouldBeFalse();
        result.Message.ShouldEqual("--engine must be one of: eventmodel, semantic.");
    }
}
