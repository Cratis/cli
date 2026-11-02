// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_PassphraseSecretsProvider;

public class when_round_tripping_a_secret : a_temp_project
{
    const string SecretValue = "s3cr3t-value-never-in-files";

    string _resolved;
    string _file;

    async Task Because()
    {
        var environment = ProjectEnvironment.Create(_environments, "production", null, PassphraseSources.Fixed("correct horse"), FastKdf);
        await environment.SetAsync("smtp:password", SecretValue, secret: true);
        await environment.SetAsync("smtp:host", "mail.example.com", secret: false);
        _file = FileText("production");

        var reopened = ProjectEnvironment.Open(_environments, "production", PassphraseSources.Fixed("correct horse"));
        _resolved = await reopened.GetValueAsync("smtp:password");
    }

    [Fact] void should_resolve_the_clear_value() => _resolved.ShouldEqual(SecretValue);
    [Fact] void should_not_write_the_clear_value_to_the_file() => _file.ShouldNotContain(SecretValue);
    [Fact] void should_write_a_secure_entry() => _file.ShouldContain("secure:");
    [Fact] void should_write_the_salt() => _file.ShouldContain("encryptionSalt:");
    [Fact] void should_keep_plain_values_readable() => _file.ShouldContain("mail.example.com");
}
