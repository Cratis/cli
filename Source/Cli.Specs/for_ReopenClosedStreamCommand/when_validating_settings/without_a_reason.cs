// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Streams;

#nullable enable

namespace Cratis.Cli.for_ReopenClosedStreamCommand.when_validating_settings;

public class without_a_reason : Specification
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    void should_require_a_non_empty_repair_reason(string? reason)
    {
        var result = new ReopenClosedStreamSettings { EventSourceId = "order-42", Reason = reason! }.Validate();
        result.Successful.ShouldBeFalse();
        result.Message.ShouldContain("--reason");
    }
}
