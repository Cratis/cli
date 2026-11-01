// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Streams;

#nullable enable

namespace Cratis.Cli.for_ReopenClosedStreamCommand.when_validating_settings;

public class with_scope_dimensions : Specification
{
    [Theory]
    [InlineData(null, null, null, null, false)]
    [InlineData("", " ", null, null, false)]
    [InlineData("order-42", null, null, null, true)]
    [InlineData(null, "Order", null, null, true)]
    [InlineData(null, null, "Accounting", null, true)]
    [InlineData(null, null, null, "2026", true)]
    void should_require_at_least_one_dimension(string? sourceId, string? sourceType, string? streamType, string? streamId, bool valid) =>
        new ReopenClosedStreamSettings { Reason = "Mistaken closure", EventSourceId = sourceId, EventSourceType = sourceType, EventStreamType = streamType, EventStreamId = streamId }.Validate().Successful.ShouldEqual(valid);
}
