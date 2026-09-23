// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_uninstalling;

public class with_dry_run : given.a_screenplay_corpus
{
    string _before;

    void Establish()
    {
        Install();
        _before = File.ReadAllText(ProjectFile(".mcp.json"));
    }

    void Because() => _result = AiCorpusSynchronizer.Uninstall(_project, dryRun: true);

    [Fact] void should_leave_the_registration_unchanged() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(_before);
    [Fact] void should_keep_the_manifest() => File.Exists(ProjectFile(".cratis/ai.manifest.json")).ShouldBeTrue();
    [Fact] void should_report_all_native_removals() => _result.Actions.Count(action => action.StartsWith("Removed MCP", StringComparison.Ordinal)).ShouldEqual(5);
}
