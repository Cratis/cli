// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensionValues;

public class when_the_environment_has_every_value : given.an_extension_model
{
    void Establish() => Establish("  setting secretKey from secret StripeKey\n");

    void Because() => Resolve(("secrets.StripeKey", "sk_live_1"), ("systems.Payments.statementDescriptor", "ACME"));

    [Fact] void should_lack_nothing() => Values.Missing.ShouldBeEmpty();
    [Fact] void should_hand_the_named_secret_to_the_setting_it_is_mapped_to() => Values.Secrets.ShouldContainOnly(new KeyValuePair<string, string>("systems__Payments__secretKey", "sk_live_1"));
    [Fact] void should_hand_the_implicit_public_setting_as_a_setting() => Values.Settings.ShouldContainOnly(new KeyValuePair<string, string>("systems__Payments__statementDescriptor", "ACME"));
}
