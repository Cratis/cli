// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Environments;
using Cratis.Cli.Extensions;

namespace Cratis.Cli.Commands.Run.for_RunExtensionValues.given;

/// <summary>
/// A model whose Payments system is realized by the Stripe extension, with a contract that also offers a public statement descriptor setting.
/// </summary>
public class an_extension_model : Extensions.given.a_restored_extension_project
{
    protected const string Tail = "\nmodule M\n  feature F\n    slice Automation S\n      event Settled\n        paymentIntentId String\n      reaction Settle\n        when Payments.PaymentSucceeded\n          paymentIntentId\n          produces Settled\n            for paymentIntentId\n            paymentIntentId = paymentIntentId\n";

    protected ModelRequirements Requirements { get; private set; }

    protected RunExtensionValues Values { get; private set; }

    protected void Establish(string systemLines, bool declareSecret = true)
    {
        var contract = ExtensionContracts.Stripe2.Replace("  setting secretKey secret", "  setting secretKey secret\n  setting statementDescriptor public", StringComparison.Ordinal);
        Restore((declareSecret ? "secret StripeKey\n  description \"The Stripe secret key\"\n\n" : string.Empty) + "system Payments\n  extension \"cratis/stripe\" version 2\n" + systemLines + Tail, contract);
        Requirements = ModelRequirements.Read(Project.Model);
    }

    protected void Resolve(params (string Key, string Value)[] values) =>
        Values = RunExtensionValues.Resolve(Requirements.Application, Requirements.Requirements, values.ToDictionary(pair => pair.Key, pair => pair.Value));
}
