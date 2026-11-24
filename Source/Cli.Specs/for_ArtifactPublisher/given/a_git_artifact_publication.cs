// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_ArtifactPublisher.given;

public class a_git_artifact_publication : an_artifact_publication
{
    async Task Establish()
    {
        Directory.CreateDirectory(_destination);
        await Git("init", "--quiet", "--template=");
    }

    protected async Task<string[]> Git(params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = _destination,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList = { "-c", "core.excludesFile=", "-c", "init.defaultBranch=main" }
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        (await error).ShouldEqual(string.Empty);
        process.ExitCode.ShouldEqual(0);
        return (await output).Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
}
