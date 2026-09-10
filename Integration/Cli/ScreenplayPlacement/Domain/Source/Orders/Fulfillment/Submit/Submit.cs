// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ScreenplayPlacement.Orders.Fulfillment.Current;
using Wolverine.Http.Marten;

namespace ScreenplayPlacement.Orders.Fulfillment.Submit;

public record SubmitOrder(Guid OrderId, string Customer);

public record OrderSubmitted(Guid OrderId, string Customer);

public static class SubmitOrderHandler
{
    public static OrderSubmitted Handle(SubmitOrder command, [Aggregate] Order order) =>
        new(command.OrderId, command.Customer);
}
