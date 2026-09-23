// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_an_escaping_root : given.a_screenplay_corpus
{
    Exception _error;

    void Establish() => _configuration = _configuration with { McpServers = new Dictionary<string, AiMcpConfiguration> { ["screenplay"] = new(true, "../outside") } };
    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_reject_the_escape() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_not_write_any_project_files() => Directory.GetFileSystemEntries(_project).ShouldBeEmpty();
}
