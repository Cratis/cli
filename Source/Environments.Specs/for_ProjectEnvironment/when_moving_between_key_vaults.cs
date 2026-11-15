// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment;

public class when_moving_between_key_vaults : given.a_key_vault_move
{
    async Task Establish()
    {
        await Create("azurekeyvault://old-vault");
        _destination = new InMemoryKeyVault();
    }

    Task Because() => Move("azurekeyvault://new-vault");

    [Fact] void should_remove_the_old_entry() => _source.Values.ContainsKey(_reference).ShouldBeFalse();
    [Fact] void should_keep_the_same_reference_in_the_new_vault() => _environment.GetEntry(Key)!.Value.ShouldEqual(_reference);
    [Fact] void should_read_the_new_entry() => _resolved.ShouldEqual(Value);
    [Fact] void should_report_one_removal() => _environment.SecretsRemoved.ShouldEqual(1);
    [Fact] void should_not_warn() => _environment.SecretRemovalWarnings.Count.ShouldEqual(0);
}
