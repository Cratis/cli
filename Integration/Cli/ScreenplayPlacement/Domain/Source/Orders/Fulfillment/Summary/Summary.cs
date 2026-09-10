// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Marten.Events.Aggregation;
using ScreenplayPlacement.Orders.Fulfillment.Submit;
using Wolverine.Http;

namespace ScreenplayPlacement.Orders.Fulfillment.Summary;

public record OrderSummary(Guid Id, string Customer);

public sealed class OrderSummaryProjection : SingleStreamProjection<OrderSummary, Guid>
{
    public OrderSummary Create(OrderSubmitted @event) => new(@event.OrderId, @event.Customer);
}

public static class OrderQueries
{
    [WolverineGet("/orders/{id}")]
    public static OrderSummary GetOrder(Guid id) => new(id, string.Empty);
}
