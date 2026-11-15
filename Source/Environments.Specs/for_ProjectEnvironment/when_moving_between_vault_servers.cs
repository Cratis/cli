// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment;

[Collection(EnvironmentSpecsCollection.Name)]
public class when_moving_between_vault_servers : given.a_vault_move
{
    Task Establish() => Create("hashivault://old.example.com/secret/cratis");
    Task Because() => Move("hashivault://new.example.com/secret/cratis");

    [Fact] void should_keep_the_old_entry() => _vault.Values.ContainsKey(_originalLocation).ShouldBeTrue();
    [Fact] void should_not_delete_any_entry() => _vault.Deletes.Count.ShouldEqual(0);
    [Fact] void should_read_the_new_entry() => _resolved.ShouldEqual(Value);
    [Fact] void should_warn_with_the_provider() => _environment.SecretRemovalWarnings.Single().ShouldContain("HashiCorpVault");
    [Fact] void should_warn_with_the_server() => _environment.SecretRemovalWarnings.Single().ShouldContain("old.example.com");
    [Fact] void should_warn_with_the_mount() => _environment.SecretRemovalWarnings.Single().ShouldContain("mount 'secret'");
    [Fact] void should_warn_with_the_key() => _environment.SecretRemovalWarnings.Single().ShouldContain(Key);
    [Fact] void should_report_no_removals() => _environment.SecretsRemoved.ShouldEqual(0);
}
