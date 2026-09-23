// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_symlinked_host_directory : given.a_screenplay_corpus
{
    Exception _error;

    void Establish() => Directory.CreateSymbolicLink(ProjectFile(".vscode"), _corpus);
    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_reject_the_link() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_not_write_outside_the_project() => File.Exists(Path.Combine(_corpus, "mcp.json")).ShouldBeFalse();
    [Fact] void should_not_install_partial_guidance() => Directory.Exists(ProjectFile(".cratis")).ShouldBeFalse();
}
