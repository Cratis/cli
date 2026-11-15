// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment;

[Collection(EnvironmentSpecsCollection.Name)]
public class when_the_vault_provider_is_unchanged : given.a_vault_move
{
    const string Provider = "hashivault://vault.example.com/secret/cratis";

    Task Establish() => Create(Provider);
    Task Because() => Move(Provider);

    [Fact] void should_keep_the_entry() => _vault.Values.ContainsKey(_originalLocation).ShouldBeTrue();
    [Fact] void should_not_delete_any_entry() => _vault.Deletes.Count.ShouldEqual(0);
    [Fact] void should_read_the_entry() => _resolved.ShouldEqual(Value);
    [Fact] void should_not_warn() => _environment.SecretRemovalWarnings.Count.ShouldEqual(0);
    [Fact] void should_report_no_removals() => _environment.SecretsRemoved.ShouldEqual(0);
}
