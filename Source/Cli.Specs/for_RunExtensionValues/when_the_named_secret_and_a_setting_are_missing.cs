// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensionValues;

public class when_the_named_secret_and_a_setting_are_missing : given.an_extension_model
{
    void Establish() => Establish("  setting secretKey from secret StripeKey\n");

    void Because() => Resolve();

    [Fact] void should_name_the_missing_keys() => Values.Missing.ShouldContainOnly("secrets.StripeKey", "systems.Payments.statementDescriptor");
    [Fact] void should_hand_over_nothing() => (Values.Settings.Count + Values.Secrets.Count).ShouldEqual(0);
}
