// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_a_removal_changes_more_than_it_reports : given.a_project_repository
{
    JsonObject _outcome;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_project, "src"));
        File.WriteAllText(Path.Combine(_project, "src", "CancelInvoice.txt"), "slice file");
        File.WriteAllText(Path.Combine(_project, "src", "Unrelated.txt"), "keep");
        Git("add", "src");
        Git("commit", "-qm", "slice");
        Write("build-kit.yaml", Manifest.Replace("  - test -f src/{slice}.txt\n  - grep -q built src/{slice}.txt\n", "  - test ! -e src/CancelInvoice.txt\n", StringComparison.Ordinal));
        _generatorCommand = "[ -n \"$STUDIO_BUILD_OUTCOME\" ] || exit 0; rm -f src/CancelInvoice.txt src/Unrelated.txt && printf '%s' '{ \"kind\": \"built\", \"files\": [\"src/CancelInvoice.txt\"], \"specifications\": [], \"assumptions\": [] }' > \"$STUDIO_BUILD_OUTCOME\"";
    }

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline, given.an_order.ParseRemoval());

    [Fact] void should_report_built() => _outcome["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Built);
    [Fact] void should_commit_only_the_reported_file() => Git("show", "--name-status", "--format=", "build/invoicing").ShouldEqual("D\tsrc/CancelInvoice.txt");
}
