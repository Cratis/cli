// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment;

[Collection(EnvironmentSpecsCollection.Name)]
public class when_moving_between_vault_namespaces : given.a_vault_move
{
    const string Provider = "hashivault://vault.example.com/secret/cratis";

    async Task Establish()
    {
        Environment.SetEnvironmentVariable("VAULT_NAMESPACE", "old");
        await Create(Provider);
        Environment.SetEnvironmentVariable("VAULT_NAMESPACE", "new");
    }

    Task Because() => Move(Provider);

    [Fact] void should_remove_the_old_entry() => _vault.Values.ContainsKey(_originalLocation).ShouldBeFalse();
    [Fact] void should_delete_only_the_old_entry() => _vault.Deletes.Single().ShouldEqual(_originalLocation);
    [Fact] void should_read_the_new_entry() => _resolved.ShouldEqual(Value);
    [Fact] void should_report_one_removal() => _environment.SecretsRemoved.ShouldEqual(1);
}
