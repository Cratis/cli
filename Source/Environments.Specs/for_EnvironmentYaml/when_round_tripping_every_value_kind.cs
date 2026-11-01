// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_EnvironmentYaml;

public class when_round_tripping_every_value_kind : Specification
{
    EnvironmentDocument _result;
    string _yaml;

    void Establish()
    {
        var document = new EnvironmentDocument
        {
            SecretsProvider = "hashivault://vault:8200/secret/cratis",
            EncryptionSalt = "c2FsdA==",
            Kdf = new KdfParameters(KdfParameters.Pbkdf2Sha256, 1234),
            EncryptionCheck = "Y2hlY2s="
        };
        document.Config["auth:clientId"] = ConfigEntry.Plain("abc");
        document.Config["numeric"] = ConfigEntry.Plain("123");
        document.Config["boolean-like"] = ConfigEntry.Plain("true");
        document.Config["empty"] = ConfigEntry.Plain(string.Empty);
        document.Config["with spaces: and colon"] = ConfigEntry.Plain("hello world # not a comment");
        document.Config["smtp:password"] = new ConfigEntry(ConfigEntryKind.Secure, "AAEC+/==");
        document.Config["signing:key"] = new ConfigEntry(ConfigEntryKind.Reference, "cratis/prod/signing:key");
        _yaml = EnvironmentYaml.Write(document);
        _result = EnvironmentYaml.Parse(_yaml);
    }

    [Fact] void should_keep_the_provider() => _result.SecretsProvider.ShouldEqual("hashivault://vault:8200/secret/cratis");
    [Fact] void should_keep_the_salt() => _result.EncryptionSalt.ShouldEqual("c2FsdA==");
    [Fact] void should_keep_the_kdf_parameters() => _result.Kdf.ShouldEqual(new KdfParameters(KdfParameters.Pbkdf2Sha256, 1234));
    [Fact] void should_keep_the_verification_value() => _result.EncryptionCheck.ShouldEqual("Y2hlY2s=");
    [Fact] void should_keep_plain_values() => _result.Config["auth:clientId"].ShouldEqual(ConfigEntry.Plain("abc"));
    [Fact] void should_keep_numeric_looking_values() => _result.Config["numeric"].Value.ShouldEqual("123");
    [Fact] void should_keep_boolean_looking_values() => _result.Config["boolean-like"].Value.ShouldEqual("true");
    [Fact] void should_keep_empty_values() => _result.Config["empty"].Value.ShouldEqual(string.Empty);
    [Fact] void should_keep_awkward_keys_and_values() => _result.Config["with spaces: and colon"].Value.ShouldEqual("hello world # not a comment");
    [Fact] void should_keep_secure_values() => _result.Config["smtp:password"].ShouldEqual(new ConfigEntry(ConfigEntryKind.Secure, "AAEC+/=="));
    [Fact] void should_keep_references() => _result.Config["signing:key"].ShouldEqual(new ConfigEntry(ConfigEntryKind.Reference, "cratis/prod/signing:key"));
    [Fact] void should_write_the_secure_marker() => _yaml.ShouldContain("secure:");
    [Fact] void should_not_leave_a_document_end_marker() => _yaml.ShouldNotContain("...");
}
