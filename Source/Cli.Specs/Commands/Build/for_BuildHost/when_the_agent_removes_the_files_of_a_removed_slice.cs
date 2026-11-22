// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_the_agent_removes_the_files_of_a_removed_slice : given.a_project_repository
{
    JsonObject _outcome;
    string _before;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_project, "src"));
        File.WriteAllText(Path.Combine(_project, "src", "CancelInvoice.txt"), "slice file");
        File.WriteAllText(Path.Combine(_project, "src", "Unrelated.txt"), "keep");
        Git("add", "src");
        Git("commit", "-qm", "slice");
        _before = Git("rev-parse", "HEAD");
        Write("build-kit.yaml", Manifest.Replace("  - test -f src/{slice}.txt\n  - grep -q built src/{slice}.txt\n", "  - test ! -e src/CancelInvoice.txt\n", StringComparison.Ordinal));
        _generatorCommand = "[ -n \"$STUDIO_BUILD_OUTCOME\" ] || exit 0; rm -f src/CancelInvoice.txt && printf '%s' '{ \"kind\": \"built\", \"files\": [\"src/CancelInvoice.txt\"], \"specifications\": [], \"assumptions\": [] }' > \"$STUDIO_BUILD_OUTCOME\"";
    }

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline, given.an_order.ParseRemoval());

    [Fact] void should_report_built() => _outcome["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Built);
    [Fact] void should_answer_with_contract_three() => _outcome["contractVersion"]!.GetValue<string>().ShouldEqual("3");
    [Fact] void should_commit_after_the_slice_was_added() => Git("rev-parse", "build/invoicing~1").ShouldEqual(_before);
    [Fact] void should_stage_the_deletion_in_the_commit() => Git("show", "--name-status", "--format=", "build/invoicing").ShouldEqual("D\tsrc/CancelInvoice.txt");
    [Fact] void should_leave_the_unrelated_file_out_of_the_commit() => Git("show", "--name-only", "--format=", "build/invoicing").ShouldNotContain("Unrelated.txt");
    [Fact] void should_keep_the_unrelated_file() => Git("cat-file", "-e", "build/invoicing:src/Unrelated.txt").ShouldBeEmpty();
    [Fact] void should_fill_the_kits_commit_message_with_the_former_name() => Git("log", "-1", "--format=%s", "build/invoicing").ShouldEqual("Build CancelInvoice");
}
