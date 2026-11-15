// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Holds contract text used by the extension specifications.
/// </summary>
static class ExtensionContracts
{
    /// <summary>The contract the model was written against: one ability and the trigger the model's reaction uses.</summary>
    public static string Stripe2 => Contract([], Trigger);

    /// <summary>Offers everything Stripe2 does, and one more ability.</summary>
    public static string Stripe3WithRefund => Contract(["  ability RefundPayment idempotent", "    paymentIntentId String", ""], Trigger);

    /// <summary>No longer raises the trigger the model's reaction uses.</summary>
    public static string Stripe3WithoutTrigger => Contract([], ["  trigger Settled", "    paymentIntentId String"]);

    static string[] Head =>
    [
        "extension Stripe",
        "  display name \"Stripe payments\"",
        "  description \"Card payments with Stripe\"",
        "  icon \"assets/icon.svg\"",
        "  image \"assets/checkout.png\"",
        "  requires stage \"^5\"",
        "  requires platform web",
        "  activation on demand idle after 10 minutes",
        "",
        "  setting secretKey secret",
        "",
        "  ability CreatePaymentIntent idempotent",
        "    amount Money",
        "    orderId String",
        "    returns",
        "      clientSecret String",
        ""
    ];

    static string[] Trigger => ["  trigger PaymentSucceeded", "    paymentIntentId String"];

    static string Contract(string[] extraAbilities, string[] trigger) =>
        string.Join('\n', [.. Head, .. extraAbilities, .. trigger, "", "  backend runtime dotnet 9 assembly \"Cratis.Extensions.Stripe.dll\""]);
}
