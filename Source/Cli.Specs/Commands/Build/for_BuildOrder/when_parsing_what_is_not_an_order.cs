// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildOrder;

public class when_parsing_what_is_not_an_order : Specification
{
    Exception _notJson;
    Exception _noOrderId;
    Exception _noSlices;

    void Because()
    {
        _notJson = Catch.Exception(() => BuildOrder.Parse("not json"));
        _noOrderId = Catch.Exception(() => BuildOrder.Parse("""{ "scope": { "slices": [ { "id": "a", "name": "A", "type": "stateChange" } ] } }"""));
        _noSlices = Catch.Exception(() => BuildOrder.Parse("""{ "orderId": "6f0c3d5e-2b1a-4f7e-9a51-0d2c8e4b7a10", "scope": { "slices": [] } }"""));
    }

    [Fact] void should_refuse_text_that_is_not_json() => _notJson.ShouldBeOfExactType<InvalidBuildOrder>();
    [Fact] void should_refuse_an_order_without_an_id() => _noOrderId.ShouldBeOfExactType<InvalidBuildOrder>();
    [Fact] void should_refuse_an_order_without_slices() => _noSlices.ShouldBeOfExactType<InvalidBuildOrder>();
}
