// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_reading_status;

public class without_the_model_directory : given.a_screenplay_corpus
{
    AiStatus _status;

    void Establish()
    {
        Install();
        Directory.Delete(ProjectFile(".cratis/screenplay"));
    }

    void Because() => _status = AiCorpusSynchronizer.Status(_project);

    [Fact] void should_report_the_missing_model_directory() => _status.ModifiedFiles.ShouldContain("MCP model directory missing: .cratis/screenplay");
    [Fact] void should_not_create_directories_from_status() => Directory.Exists(ProjectFile(".cratis/screenplay")).ShouldBeFalse();
}
