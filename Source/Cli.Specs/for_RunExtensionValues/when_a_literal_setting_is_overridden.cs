// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensionValues;

public class when_a_literal_setting_is_overridden : given.an_extension_model
{
    RunExtensionValues _withoutOverride;

    void Establish() => Establish("  setting secretKey from secret StripeKey\n  setting statementDescriptor = \"Model\"\n");

    void Because()
    {
        Resolve(("secrets.StripeKey", "sk_live_1"));
        _withoutOverride = Values;
        Resolve(("secrets.StripeKey", "sk_live_1"), ("systems.Payments.statementDescriptor", "Environment"));
    }

    [Fact] void should_use_the_literal_of_the_model_without_an_override() => _withoutOverride.Settings.ShouldContainOnly(new KeyValuePair<string, string>("systems__Payments__statementDescriptor", "Model"));
    [Fact] void should_use_the_environment_value_when_it_overrides() => Values.Settings.ShouldContainOnly(new KeyValuePair<string, string>("systems__Payments__statementDescriptor", "Environment"));
    [Fact] void should_not_ask_for_a_literal_that_is_mapped() => _withoutOverride.Missing.ShouldBeEmpty();
}
