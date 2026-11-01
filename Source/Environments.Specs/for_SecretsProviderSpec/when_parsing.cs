// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_SecretsProviderSpec;

public class when_parsing : Specification
{
    [Fact] void should_default_to_passphrase() => SecretsProviderSpec.Parse(null).Kind.ShouldEqual(SecretsProviderKind.Passphrase);
    [Fact] void should_parse_passphrase() => SecretsProviderSpec.Parse("passphrase").Kind.ShouldEqual(SecretsProviderKind.Passphrase);
    [Fact] void should_parse_azure_key_vault() => SecretsProviderSpec.Parse("azurekeyvault://my-vault").Target.ShouldEqual("my-vault");
    [Fact] void should_accept_the_hyphenated_alias() => SecretsProviderSpec.Parse("azure-keyvault://my-vault").Text.ShouldEqual("azurekeyvault://my-vault");
    [Fact] void should_parse_hashicorp_vault() => SecretsProviderSpec.Parse("hashivault://vault.example.com:8200/secret/cratis").Kind.ShouldEqual(SecretsProviderKind.HashiCorpVault);
    [Fact] void should_require_a_mount_for_hashicorp_vault() => Catch.Exception(() => SecretsProviderSpec.Parse("hashivault://vault.example.com:8200")).ShouldBeOfExactType<EnvironmentException>();
    [Fact] void should_reject_unknown_providers() => Catch.Exception(() => SecretsProviderSpec.Parse("plaintext")).ShouldBeOfExactType<EnvironmentException>();
}
