// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_file_at_the_model_root : given.a_screenplay_corpus
{
    Exception _error;

    void Establish()
    {
        _configuration = _configuration with
        {
            Harnesses = ["claude"],
            McpServers = new Dictionary<string, AiMcpConfiguration> { ["screenplay"] = new(true, "model") }
        };
        Write("model", "Owned by the user, not a model directory.\n");
    }

    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_reject_the_file_with_a_locatable_error() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_name_the_conflicting_root() => _error.Message.ShouldContain("model");
    [Fact] void should_keep_the_user_file() => File.ReadAllText(ProjectFile("model")).ShouldEqual("Owned by the user, not a model directory.\n");
    [Fact] void should_not_register_the_server() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
}
