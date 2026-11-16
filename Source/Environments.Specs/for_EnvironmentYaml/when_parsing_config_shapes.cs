// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_EnvironmentYaml;

public class when_parsing_config_shapes : Specification
{
    EnvironmentDocument _document;

    void Because() => _document = EnvironmentYaml.Parse(
        "secretsProvider: [ignored]\n" +
        "encryptionSalt: [ignored]\n" +
        "encryption: ignored\n" +
        "unknown: ignored\n" +
        "config:\n" +
        "  ? [non, scalar]\n" +
        "  : ignored\n" +
        "  plain: visible\n" +
        "  empty:\n" +
        "  sequence: [ignored]\n" +
        "  secure: {secure: protected, ref: ignored}\n" +
        "  reference: {secure: [ignored], ref: remote}");

    [Fact] void should_default_a_non_scalar_provider() => _document.SecretsProvider.ShouldEqual(EnvironmentDocument.PassphraseProvider);
    [Fact] void should_ignore_a_non_scalar_salt() => _document.EncryptionSalt.ShouldBeNull();
    [Fact] void should_ignore_non_mapping_encryption() => _document.Kdf.ShouldBeNull();
    [Fact] void should_skip_non_scalar_keys() => _document.Config.Count.ShouldEqual(5);
    [Fact] void should_read_plain_values() => _document.Config["plain"].ShouldEqual(ConfigEntry.Plain("visible"));
    [Fact] void should_read_empty_values() => _document.Config["empty"].ShouldEqual(ConfigEntry.Plain(string.Empty));
    [Fact] void should_treat_sequences_as_empty_values() => _document.Config["sequence"].ShouldEqual(ConfigEntry.Plain(string.Empty));
    [Fact] void should_prefer_a_secure_scalar_over_a_reference() => _document.Config["secure"].ShouldEqual(new ConfigEntry(ConfigEntryKind.Secure, "protected"));
    [Fact] void should_read_a_reference_when_secure_is_not_a_scalar() => _document.Config["reference"].ShouldEqual(new ConfigEntry(ConfigEntryKind.Reference, "remote"));
}
