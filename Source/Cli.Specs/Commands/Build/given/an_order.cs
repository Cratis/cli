// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.given;

public static class an_order
{
    public const string OrderId = "6f0c3d5e-2b1a-4f7e-9a51-0d2c8e4b7a10";
    public const string SliceId = "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f";
    public const string SpecificationId = "8e1f2a3b-4c5d-4e6f-8a9b-0c1d2e3f4a5b";

    public static string Json(string type = "stateChange", string part = "backend") => new JsonObject
    {
        ["contractVersion"] = "1",
        ["orderId"] = OrderId,
        ["part"] = part,
        ["scope"] = new JsonObject
        {
            ["application"] = new JsonObject { ["id"] = "0b9d1f0e-6a4c-4c1e-8f5a-3e2d1c0b9a87", ["name"] = "Shop" },
            ["feature"] = new JsonObject { ["id"] = "4a7e2c91-5d3b-4e8f-a1c2-9b8d7e6f5a40", ["name"] = "Invoicing", ["path"] = new JsonArray("Billing") },
            ["slices"] = new JsonArray(new JsonObject { ["id"] = SliceId, ["name"] = "PayInvoice", ["type"] = type, ["status"] = "readyForImplementation" })
        },
        ["model"] = new JsonObject { ["language"] = "screenplay", ["schemaVersion"] = "1", ["documents"] = new JsonArray(new JsonObject { ["path"] = "Billing.play", ["content"] = "module Billing" }) },
        ["specifications"] = new JsonArray(new JsonObject { ["sliceId"] = SliceId, ["id"] = SpecificationId, ["name"] = "Paying an open invoice" }),
        ["context"] = new JsonObject { ["attempt"] = 2 }
    }.ToJsonString();

    public static BuildOrder Parse(string type = "stateChange", string part = "backend") => BuildOrder.Parse(Json(type, part));
}
