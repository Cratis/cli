// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ScreenplayPlacement.Orders.Fulfillment.Submit;

namespace ScreenplayPlacement.Orders.Fulfillment.Notify;

public record SendOrderConfirmation(Guid OrderId, string Customer);

public static class OrderConfirmationHandler
{
    public static SendOrderConfirmation Handle(OrderSubmitted @event) =>
        new(@event.OrderId, @event.Customer);
}
