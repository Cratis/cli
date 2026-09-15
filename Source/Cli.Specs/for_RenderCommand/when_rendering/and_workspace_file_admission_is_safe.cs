// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Cratis.Screenplay.Workspaces;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public partial class and_workspace_file_admission_is_safe : given.a_workspace_render_command
{
    [Fact]
    public async Task should_read_a_normal_file_through_the_admitted_handle()
    {
        var workspace = CreateWorkspace();
        WriteWorkspace(workspace);

        var result = await RenderWorkspaceInput.Read(_input, CancellationToken.None);

        ScreenplayWorkspaceSerializer.Serialize(result).SequenceEqual(ScreenplayWorkspaceSerializer.Serialize(workspace)).ShouldBeTrue();
    }

    [Fact]
    public void should_reject_a_directory()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Throws<UnauthorizedAccessException>(() => WorkspaceFile.Open(_folder, CancellationToken.None));
        }
        else
        {
            Assert.Throws<UnsafeWorkspaceInput>(() => WorkspaceFile.Open(_folder, CancellationToken.None));
        }
    }

    [Fact]
    public void should_preserve_missing_file_errors() => Assert.Throws<FileNotFoundException>(() => WorkspaceFile.Open(_input, CancellationToken.None));

    [Fact]
    public void should_reject_embedded_nul_before_native_path_truncation()
    {
        WriteWorkspace(CreateWorkspace());

        Assert.Throws<UnsafeWorkspaceInput>(() => WorkspaceFile.Open(_input + "\0ignored", CancellationToken.None));
    }

    [Fact]
    public void should_observe_cancellation_before_opening()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => WorkspaceFile.Open(_input, cancellation.Token));
    }

    [Unix.Fact]
    public void should_treat_descriptor_zero_as_a_valid_unix_handle()
    {
        using var handle = new SafeFileHandle(nint.Zero, ownsHandle: false);
        handle.IsInvalid.ShouldBeFalse();
    }

    [Unix.Fact]
    [UnsupportedOSPlatform("windows")]
    public async Task should_allow_a_read_only_symlink_target_after_handle_validation()
    {
        WriteWorkspace(CreateWorkspace());
        File.SetUnixFileMode(_input, UnixFileMode.UserRead);
        var link = Path.Combine(_folder, "link.workspace.json");
        File.CreateSymbolicLink(link, _input);

        var result = await RenderWorkspaceInput.Read(link, CancellationToken.None);

        result.ApplicationName.ShouldEqual(CreateWorkspace().ApplicationName);
    }

    [Unix.Fact]
    public void should_reject_a_character_device() => Assert.Throws<UnsafeWorkspaceInput>(() => WorkspaceFile.Open("/dev/null", CancellationToken.None));

    [Unix.Fact]
    public void should_reject_a_unix_socket()
    {
        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var path = Path.Combine(_folder, "s");
        var previousDirectory = Directory.GetCurrentDirectory();
        try
        {
            // Bind a relative name so a long managed task path cannot exceed sockaddr_un's limit.
            Directory.SetCurrentDirectory(_folder);
            socket.Bind(new UnixDomainSocketEndPoint("s"));
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
        }

        Assert.Throws<UnsafeWorkspaceInput>(() => WorkspaceFile.Open(path, CancellationToken.None));
    }

    [Unix.Fact]
    public async Task should_reject_a_fifo_without_a_writer_in_a_deadline_bounded_cli_child()
    {
        CreateFifo(_input);
        var specs = typeof(and_workspace_file_admission_is_safe).Assembly.Location;
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = _folder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Execute the built CLI entry point using the test output's resolved dependency closure.
        // No probe program or hidden product flag is needed, and a regression cannot hang the test host.
        string[] arguments = ["exec", "--runtimeconfig", Path.ChangeExtension(specs, ".runtimeconfig.json"), "--depsfile", Path.ChangeExtension(specs, ".deps.json"), typeof(RenderCommand).Assembly.Location,
            "render", "--workspace", _input, "--destination", _destination, "-o", "json-compact"];
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var child = new Process { StartInfo = start };
        child.Start().ShouldBeTrue();
        var output = child.StandardOutput.ReadToEndAsync();
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await child.WaitForExitAsync(deadline.Token);
            child.ExitCode.ShouldEqual(ExitCodes.ValidationError);
            Assert.Contains("Workspace input", await errors, StringComparison.Ordinal);
            (await output).ShouldEqual(string.Empty);
            Directory.Exists(_destination).ShouldBeFalse();
        }
        finally
        {
            if (!child.HasExited)
            {
                child.Kill(entireProcessTree: true);
            }

            // Always reap the disposable child, including a blocked-open regression.
            await child.WaitForExitAsync();
            await Task.WhenAll(output, errors);
        }
    }

    [Unix.Fact]
    public void should_reject_the_opened_fifo_even_after_its_path_becomes_a_regular_file()
    {
        CreateFifo(_input);
        using var handle = OpenNonblocking(_input);
        File.Move(_input, _input + ".old");
        WriteWorkspace(CreateWorkspace());

        Assert.Throws<UnsafeWorkspaceInput>(() => WorkspaceFile.Validate(handle, CancellationToken.None));
        handle.IsClosed.ShouldBeFalse(); // Validation never takes ownership from its caller.
    }

    [Unix.Fact]
    public async Task should_read_the_opened_regular_file_even_after_its_path_becomes_a_fifo()
    {
        var workspace = CreateWorkspace();
        WriteWorkspace(workspace);
        using var handle = OpenNonblocking(_input);
        File.Move(_input, _input + ".old");
        CreateFifo(_input);

        WorkspaceFile.Validate(handle, CancellationToken.None);
        await using var stream = new FileStream(handle, FileAccess.Read);
        var result = await RenderWorkspaceInput.Read(stream, CancellationToken.None);

        ScreenplayWorkspaceSerializer.Serialize(result).SequenceEqual(ScreenplayWorkspaceSerializer.Serialize(workspace)).ShouldBeTrue();
    }

    [Windows.Theory]
    [InlineData(@"\\.\PhysicalDrive0")]
    [InlineData(@"\\?\GLOBALROOT\Device\HarddiskVolume1\input.json")]
    [InlineData(@"\??\C:\input.json")]
    [InlineData(@"C:\input\NUL.json")]
    [InlineData(@"C:\input\COM1")]
    [InlineData(@"C:\input\LPT¹.txt")]
    [InlineData(@"C:\input\CONIN$")]
    [InlineData(@"C:\input\workspace.json:stream")]
    public void should_reject_windows_device_and_raw_namespace_paths_before_open(string path) =>
        Assert.Throws<UnsafeWorkspaceInput>(() => WorkspaceFile.Open(path, CancellationToken.None));

    [Windows.Fact]
    public void should_reject_an_opened_windows_pipe()
    {
        using var pipe = new NamedPipeServerStream($"workspace-admission-{Guid.NewGuid():N}");
        using var handle = new SafeFileHandle(pipe.SafePipeHandle.DangerousGetHandle(), ownsHandle: false);

        Assert.Throws<UnsafeWorkspaceInput>(() => WorkspaceFile.Validate(handle, CancellationToken.None));
    }

    static void CreateFifo(string path) => Assert.Equal(0, MakeFifo(path, 0x180));

    static SafeFileHandle OpenNonblocking(string path)
    {
        var flags = OperatingSystem.IsLinux() ? 0x800 | 0x80000 | 0x100 : 0x4 | 0x1000000 | 0x20000;
        var descriptor = OpenUnix(path, flags);
        Assert.True(descriptor >= 0, $"Nonblocking fixture open failed: errno {Marshal.GetLastPInvokeError()}");
        return new SafeFileHandle(descriptor, ownsHandle: true);
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "mkfifo", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int MakeFifo(string path, uint mode);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int OpenUnix(string path, int flags);

    /// <summary>
    /// Actual xUnit facts with explicit Unix skips and names recognized by the specification analyzer.
    /// </summary>
    internal static class Unix
    {
        public sealed class FactAttribute : Xunit.FactAttribute
        {
            public FactAttribute()
            {
                if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
                {
                    Skip = "Requires Linux or macOS native file admission.";
                }
            }
        }
    }

    internal static class Windows
    {
        public sealed class FactAttribute : Xunit.FactAttribute
        {
            public FactAttribute()
            {
                if (!OperatingSystem.IsWindows())
                {
                    Skip = "Requires Windows handle types.";
                }
            }
        }

        public sealed class TheoryAttribute : Xunit.TheoryAttribute
        {
            public TheoryAttribute()
            {
                if (!OperatingSystem.IsWindows())
                {
                    Skip = "Requires Windows path semantics.";
                }
            }
        }
    }
}
