// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_EnvironmentYaml;

public class when_parsing_non_mapping_documents : Specification
{
    EnvironmentDocument[] _documents;

    void Because()
    {
        string[] yaml = [string.Empty, "plain", "- first\n- second"];
        _documents = [.. yaml.Select(EnvironmentYaml.Parse)];
    }

    [Fact] void should_use_the_default_provider() => _documents.All(document => document.SecretsProvider == EnvironmentDocument.PassphraseProvider).ShouldBeTrue();
    [Fact] void should_leave_configuration_empty() => _documents.All(document => document.Config.Count == 0).ShouldBeTrue();
}
