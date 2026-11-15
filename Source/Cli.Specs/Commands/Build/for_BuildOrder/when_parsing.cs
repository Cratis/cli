// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildOrder;

public class when_parsing : Specification
{
    BuildOrder _order;

    void Because() => _order = given.an_order.Parse();

    [Fact] void should_read_the_order_id() => _order.OrderId.ShouldEqual(given.an_order.OrderId);
    [Fact] void should_read_the_part() => _order.Part.ShouldEqual("backend");
    [Fact] void should_read_the_application() => _order.Application.ShouldEqual("Shop");
    [Fact] void should_read_the_feature_path_down_to_the_feature() => _order.FeaturePath.ToArray().ShouldEqual(["Billing", "Invoicing"]);
    [Fact] void should_read_the_slice() => _order.Slices.Single().ShouldEqual(new BuildOrderSlice(given.an_order.SliceId, "PayInvoice", "stateChange"));
    [Fact] void should_read_the_attempt() => _order.Attempt.ShouldEqual(2);
    [Fact] void should_read_the_screenplay_documents() => _order.Documents.Single().ShouldEqual(new BuildOrderDocument("Billing.play", "module Billing"));
}
