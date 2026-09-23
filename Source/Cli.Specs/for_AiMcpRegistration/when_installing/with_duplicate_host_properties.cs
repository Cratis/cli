// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_duplicate_host_properties : given.a_screenplay_corpus
{
    const string Original = "{\n  // user-owned\n  \"mcpServers\": {\"another\": {\"command\": \"keep\"}},\n  \"mcpServers\": {\"screenplay\": {\"command\": \"other\"}}\n}\n";
    Exception _error;

    void Establish()
    {
        _configuration = _configuration with { Harnesses = ["claude"] };
        Write(".mcp.json", Original);
    }

    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_refuse_ambiguous_configuration() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_preserve_the_entire_user_file() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Original);
    [Fact] void should_not_create_the_model_root() => Directory.Exists(ProjectFile(".cratis/screenplay")).ShouldBeFalse();
}
