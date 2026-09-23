// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_an_invalid_project_setting : given.a_screenplay_corpus
{
    Exception _error;

    void Establish() => Write(".cratis/ai.json", """{"mcpServers":{"screenplay":{"enabled":"false"}}}""");
    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_reject_a_string_boolean_instead_of_silently_enabling() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_write_no_host_configuration() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
}
