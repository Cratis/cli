// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment;

[Collection(EnvironmentSpecsCollection.Name)]
public class when_moving_to_a_vault_alias : given.a_vault_move
{
    Task Establish() => Create("hashivault://vault.example.com/secret/cratis");
    Task Because() => Move("HASHIVAULT://VAULT.EXAMPLE.COM:443/secret/cratis/");

    [Fact] void should_keep_the_old_entry() => _vault.Values.ContainsKey(_originalLocation).ShouldBeTrue();
    [Fact] void should_not_delete_any_entry() => _vault.Deletes.Count.ShouldEqual(0);
    [Fact] void should_read_the_new_entry() => _resolved.ShouldEqual(Value);
    [Fact] void should_warn_that_the_entry_was_kept() => _environment.SecretRemovalWarnings.Single().ShouldContain("Kept previous");
    [Fact] void should_report_no_removals() => _environment.SecretsRemoved.ShouldEqual(0);
}
