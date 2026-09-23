// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_malformed_host_json : given.a_screenplay_corpus
{
    Exception _error;

    void Establish() => Write(".vscode/mcp.json", "{broken");
    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_fail_before_writing() => _error.ShouldNotBeNull();
    [Fact] void should_preserve_the_invalid_document() => File.ReadAllText(ProjectFile(".vscode/mcp.json")).ShouldEqual("{broken");
    [Fact] void should_not_install_partial_guidance() => Directory.Exists(ProjectFile(".cratis")).ShouldBeFalse();
}
