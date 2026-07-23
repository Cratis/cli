// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliConfiguration;

[Collection(CliSpecsCollection.Name)]
public class when_saving_config_with_llm_section : given.a_temp_config_directory
{
    const string ExpectedKind = "anthropic";
    const string ExpectedEndpoint = "http://localhost:11434/v1";
    const string ExpectedApiKey = "sk-ant-test-key";
    const string ExpectedModel = "claude-opus-4-6";

    CliConfiguration _loaded;
    string _json;

    void Establish()
    {
        var config = new CliConfiguration
        {
            Llm = new LlmConfiguration
            {
                Kind = ExpectedKind,
                Endpoint = ExpectedEndpoint,
                ApiKey = ExpectedApiKey,
                Model = ExpectedModel
            }
        };
        config.Save();
    }

    void Because()
    {
        _json = File.ReadAllText(CliConfiguration.GetConfigPath());
        _loaded = CliConfiguration.Load();
    }

    [Fact] void should_roundtrip_the_kind() => _loaded.Llm.Kind.ShouldEqual(ExpectedKind);
    [Fact] void should_roundtrip_the_endpoint() => _loaded.Llm.Endpoint.ShouldEqual(ExpectedEndpoint);
    [Fact] void should_roundtrip_the_api_key() => _loaded.Llm.ApiKey.ShouldEqual(ExpectedApiKey);
    [Fact] void should_roundtrip_the_model() => _loaded.Llm.Model.ShouldEqual(ExpectedModel);
    [Fact] void should_serialize_the_section_with_camel_case() => _json.ShouldContain("\"llm\"");
}
