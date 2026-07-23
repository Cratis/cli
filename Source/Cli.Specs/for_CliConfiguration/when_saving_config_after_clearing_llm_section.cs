// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliConfiguration;

[Collection(CliSpecsCollection.Name)]
public class when_saving_config_after_clearing_llm_section : given.a_temp_config_directory
{
    CliConfiguration _loaded;
    string _json;

    void Establish()
    {
        var config = new CliConfiguration
        {
            Llm = new LlmConfiguration { Kind = "anthropic", ApiKey = "sk-ant-test-key" }
        };
        config.Save();

        config.Llm = null;
        config.Save();
    }

    void Because()
    {
        _json = File.ReadAllText(CliConfiguration.GetConfigPath());
        _loaded = CliConfiguration.Load();
    }

    [Fact] void should_not_have_an_llm_section() => _loaded.Llm.ShouldBeNull();
    [Fact] void should_not_write_the_section_to_the_file() => _json.ShouldNotContain("\"llm\"");
}
