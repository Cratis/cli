// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment;

public class when_moving_to_a_key_vault_alias : given.a_key_vault_move
{
    Task Establish() => Create("azurekeyvault://my-vault");
    Task Because() => Move("AZURE-KEYVAULT://HTTPS://MY-VAULT.VAULT.AZURE.NET:443/");

    [Fact] void should_keep_the_entry() => _source.Values.ContainsKey(_reference).ShouldBeTrue();
    [Fact] void should_read_the_new_entry() => _resolved.ShouldEqual(Value);
    [Fact] void should_warn_with_the_old_entry() => _environment.SecretRemovalWarnings.Single().ShouldContain(_reference);
    [Fact] void should_report_no_removals() => _environment.SecretsRemoved.ShouldEqual(0);
}
