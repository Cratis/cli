// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensionValues;

public class when_a_setting_is_implicit : given.an_extension_model
{
    void Establish() => Establish(string.Empty, declareSecret: false);

    void Because() => Resolve(("systems.Payments.secretKey", "sk_live_2"), ("systems.Payments.statementDescriptor", "ACME"));

    [Fact] void should_read_the_secret_setting_from_the_system_key_and_keep_it_secret() => Values.Secrets.ShouldContainOnly(new KeyValuePair<string, string>("systems__Payments__secretKey", "sk_live_2"));
    [Fact] void should_not_mark_the_public_setting_secret() => Values.Settings.ShouldContainOnly(new KeyValuePair<string, string>("systems__Payments__statementDescriptor", "ACME"));
}
