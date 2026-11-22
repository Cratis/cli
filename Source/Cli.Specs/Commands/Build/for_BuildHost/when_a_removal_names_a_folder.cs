// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_a_removal_names_a_folder : given.a_project_repository
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
        _generatorCommand = "[ -n \"$STUDIO_BUILD_OUTCOME\" ] || exit 0; rm -f src/CancelInvoice.txt src/Unrelated.txt && printf '%s' '{ \"kind\": \"built\", \"files\": [\"src\"], \"specifications\": [], \"assumptions\": [] }' > \"$STUDIO_BUILD_OUTCOME\"";
    }

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline, given.an_order.ParseRemoval());

    [Fact] void should_fail_the_removal() => _outcome["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Failed);
    [Fact] void should_say_a_folder_was_named() => _outcome["error"]!.GetValue<string>().ShouldContain("named a folder");
    [Fact] void should_commit_nothing() => Git("rev-parse", "HEAD").ShouldEqual(_before);
}
