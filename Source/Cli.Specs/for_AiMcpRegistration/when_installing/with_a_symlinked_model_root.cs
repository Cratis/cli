// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_symlinked_model_root : given.a_screenplay_corpus
{
    Exception _error;

    void Establish()
    {
        Directory.CreateDirectory(ProjectFile(".cratis"));
        Directory.CreateSymbolicLink(ProjectFile(".cratis/screenplay"), _corpus);
    }

    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_reject_the_model_link() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_not_register_a_server_for_the_wrong_model() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
}
