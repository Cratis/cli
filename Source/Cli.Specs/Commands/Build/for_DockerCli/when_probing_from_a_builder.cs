// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Cratis.Cli.Commands.Run;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Cli.Commands.Build.for_DockerCli;

[Collection(CliSpecsCollection.Name)]
public partial class when_probing_from_a_builder : given.a_builder_process
{
    [Cratis.Cli.for_DirectFileSecrets.given.unix_only.Fact]
    async Task should_isolate_both_probe_modes_from_credentials_and_host_input()
    {
        Script("docker", Check + " && ! read -r line && echo ready");
        Set("PATH", Folder);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var input = Path.Combine(Folder, "host-input");
        await File.WriteAllTextAsync(input, "host-sentinel\n", deadline.Token);
        using var runInput = File.OpenHandle(input);
        using var executeInput = File.OpenHandle(input);
        using var saved = new SafeFileHandle(Duplicate(0), ownsHandle: true);
        saved.IsInvalid.ShouldBeFalse();
        try
        {
            // Process.Start inherits descriptor zero, not Console.In. Give each mode the same unread line.
            Replace(runInput, 0).ShouldEqual(0);
            (await DockerCli.Run(["images"], deadline.Token)).Trim().ShouldEqual("ready");
            Replace(executeInput, 0).ShouldEqual(0);
            (await DockerCli.Execute(["images"], deadline.Token)).ExitCode.ShouldEqual(0);
        }
        finally
        {
            Replace(saved, 0).ShouldEqual(0);
        }
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "dup")]
    private static partial int Duplicate(int descriptor);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "dup2")]
    private static partial int Replace(SafeFileHandle source, int target);
}
