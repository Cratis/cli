// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments.for_ModelRequirements;

public class when_reading_a_model_with_a_named_secret : Extensions.given.a_restored_extension_project
{
    const string Model = "secret StripeKey\n  description \"The Stripe secret key\"\n\nsystem Payments\n  extension \"cratis/stripe\" version 2\n  setting secretKey from secret StripeKey\n\nmodule M\n  feature F\n    slice Automation S\n      event Settled\n        paymentIntentId String\n      reaction Settle\n        when Payments.PaymentSucceeded\n          paymentIntentId\n          produces Settled\n            for paymentIntentId\n            paymentIntentId = paymentIntentId\n";

    ModelRequirements _requirements;

    void Establish() => Restore(Model);

    void Because() => _requirements = ModelRequirements.Read(Project.Model);

    [Fact] void should_need_the_named_secret_once() => _requirements.Requirements.Keys.Select(key => key.Key).ShouldContainOnly("secrets.StripeKey");
    [Fact] void should_describe_it_as_the_model_does() => _requirements.Requirements.Keys.Single().Description.ShouldEqual("The Stripe secret key");
    [Fact] void should_mark_it_as_a_secret() => _requirements.Requirements.Keys.Single().IsSecret.ShouldBeTrue();
}
