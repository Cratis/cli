// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Marten.Events.Aggregation;
using ScreenplayPlacement.Orders.Fulfillment.Submit;

namespace ScreenplayPlacement.Orders.Fulfillment.Current;

public class Order
{
    public Guid Id { get; set; }

    public string Customer { get; set; } = string.Empty;

    public void Apply(OrderSubmitted @event)
    {
        Id = @event.OrderId;
        Customer = @event.Customer;
    }
}

public sealed class OrderProjection : SingleStreamProjection<Order, Guid>
{
    public Order Create(OrderSubmitted @event) =>
        new()
        {
            Id = @event.OrderId,
            Customer = @event.Customer
        };
}
