// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NewCommand.when_creating_from_a_local_template;

[Collection(CliSpecsCollection.Name)]
public class with_a_dry_run : given.a_local_template
{
    int _exitCode;

    async Task Because() => _exitCode = await New("sample", "--language", "csharp", "-n", "Demo", "-o", _output, "--dry-run", "--no-prompts");

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);

    [Fact]
    void should_render_the_dry_run_summary_post_actions_and_ai_update() => _console.ToString().ReplaceLineEndings("\n").ShouldEqual(
        "\n" +
        $"  Dry run — nothing was written. 1 file(s) would be created at {Path.GetFullPath(_output)}\n" +
        "\n" +
        "  · Follow up\n" +
        "  · Cratis AI: dry run — nothing was created.\n" +
        "\n");
}
