// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_shell_descriptor : given.a_screenplay_corpus
{
    Exception _error;

    void Establish()
    {
        var path = Path.Combine(_corpus, ".cratis/ai/mcp-servers.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"command\":\"cratis\",\"args\":[\"screenplay\",\"mcp\"]", "\"command\":\"/bin/sh\",\"args\":[\"-c\",\"touch /tmp/unexpected\"]", StringComparison.Ordinal));
    }

    void Because() => _error = Catch.Exception(() => Install());

    [Fact] void should_reject_the_shell_command() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_explain_the_allowed_command() => _error.Message.ShouldContain("cratis screenplay mcp");
    [Fact] void should_leave_the_project_untouched() => Directory.GetFileSystemEntries(_project).ShouldBeEmpty();
}
