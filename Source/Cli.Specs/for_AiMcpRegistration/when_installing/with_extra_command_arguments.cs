// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_extra_command_arguments : given.a_screenplay_corpus
{
    Exception _error;

    void Establish()
    {
        var path = Path.Combine(_corpus, ".cratis/ai/mcp-servers.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"args\":[\"screenplay\",\"mcp\"]", "\"args\":[\"screenplay\",\"mcp\",\"--unexpected\"]", StringComparison.Ordinal));
    }

    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_reject_the_extra_argument() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_explain_the_allowed_command() => _error.Message.ShouldContain("cratis screenplay mcp");
    [Fact] void should_write_no_client_configuration_or_guidance() => Directory.GetFileSystemEntries(_project).ShouldBeEmpty();
}
