// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Rewrites only the verified, opened configuration inode, never a link's destination.</summary>
internal static partial class AiConfigurationFile
{
    /// <summary>Checks the existing file again through a no-follow handle before rewriting it.</summary>
    /// <param name="path">The existing configuration.</param>
    /// <param name="content">The new document.</param>
    /// <param name="originalBytes">The exact document captured by the plan.</param>
    /// <param name="beforeReplace">The original document's path/content precondition.</param>
    /// <param name="beforeOpen">An optional interleaving seam after the final path check.</param>
    /// <param name="write">An optional failing-write seam.</param>
    /// <param name="writing">Marks that a write may have changed the verified file.</param>
    /// <exception cref="IOException">When the opened configuration cannot be verified or written safely.</exception>
    internal static void Write(string path, string content, byte[] originalBytes, Action? beforeReplace, Action<string>? beforeOpen, Action<Stream, string>? write, Action writing)
    {
        var parents = new List<SafeFileHandle>();
        try
        {
            var parent = OpenParent(path, parents);
            using var original = Open(path, parent, writable: false);
            var expectedIdentity = Identity(original);
            beforeReplace?.Invoke();
            beforeOpen?.Invoke(path);
            using var current = Open(path, parent, writable: true);
            if (Identity(current) != expectedIdentity || !Read(current).AsSpan().SequenceEqual(originalBytes))
            {
                throw new IOException($"{path} changed during MCP installation; retry after reviewing it.");
            }
            using var stream = new AiConfigurationWriteStream(current, writing);
            if (write is not null) write(stream, content);
            else stream.Write(Encoding.UTF8.GetBytes(content));
            stream.Flush();
            stream.SetLength(stream.Position);
            stream.Flush();
        }
        catch (Exception error) when (error is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new IOException("The platform API needed to verify Direct MCP configuration is unavailable; no content was written.", error);
        }
        finally
        {
            foreach (var parent in parents) parent.Dispose();
        }
    }

    /// <summary>Gets Linux no-follow flags for the shipped architectures, including search-only directories.</summary>
    /// <param name="architecture">The native architecture.</param>
    /// <param name="directory">Whether this is a parent directory.</param>
    /// <param name="writable">Whether the file will be written.</param>
    /// <returns>The flags for openat.</returns>
    /// <exception cref="IOException">When the architecture is not supported.</exception>
    internal static int LinuxOpenFlags(Architecture architecture, bool directory, bool writable)
    {
        var (noFollow, directoryFlag) = architecture switch
        {
            Architecture.X64 => (0x20000, 0x10000),
            Architecture.Arm64 => (0x8000, 0x4000),
            _ => throw new IOException("No-follow configuration writes are unsupported on this Linux architecture.")
        };
        return noFollow | 0x80000 | (directory ? 0x200000 | directoryFlag : 0x800 | (writable ? 2 : 0)); // CLOEXEC, O_PATH or NONBLOCK/RDWR.
    }

    /// <summary>Gets the statx syscall number without depending on glibc versioned exports.</summary>
    /// <param name="architecture">The native architecture.</param>
    /// <returns>The architecture's syscall number.</returns>
    /// <exception cref="IOException">When the architecture is not supported.</exception>
    internal static long LinuxStatxNumber(Architecture architecture) => architecture switch
    {
        Architecture.X64 => 332,
        Architecture.Arm64 => 291,
        _ => throw new IOException("Configuration file identity inspection is unsupported on this Linux architecture.")
    };

    static byte[] Read(SafeFileHandle handle)
    {
        using var bytes = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count = RandomAccess.Read(handle, buffer, bytes.Length)) > 0) bytes.Write(buffer, 0, count);
        return bytes.ToArray();
    }

    static SafeFileHandle OpenParent(string path, List<SafeFileHandle> parents)
    {
        var full = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var current = Path.GetPathRoot(full)!;
        var parent = OperatingSystem.IsWindows() ? OpenWindows(current, writable: false, directory: true) : OpenUnix(-100, "/", directory: true, writable: false);
        parents.Add(parent);
        foreach (var segment in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            parent = OperatingSystem.IsWindows()
                ? OpenWindows(current, writable: false, directory: true)
                : OpenUnix(parent.DangerousGetHandle().ToInt32(), segment, directory: true, writable: false);
            parents.Add(parent);
        }
        return parent;
    }

    static SafeFileHandle Open(string path, SafeFileHandle parent, bool writable) => OperatingSystem.IsWindows()
        ? OpenWindows(path, writable, directory: false)
        : OpenUnix(parent.DangerousGetHandle().ToInt32(), Path.GetFileName(path), directory: false, writable);

    static SafeFileHandle OpenUnix(int parent, string name, bool directory, bool writable)
    {
        var flags = writable ? 2 : 0; // O_RDWR / O_RDONLY, never O_TRUNC or O_CREAT.
        if (OperatingSystem.IsLinux()) flags = LinuxOpenFlags(RuntimeInformation.ProcessArchitecture, directory, writable);
        else if (OperatingSystem.IsMacOS()) flags |= 0x100 | 0x1000000 | 4 | (directory ? 0x40100000 : 0); // NOFOLLOW, CLOEXEC, NONBLOCK, O_SEARCH.
        else throw new IOException("No-follow configuration writes are unsupported on this platform.");
        var descriptor = OpenAt(parent, name, flags);
        if (descriptor < 0) throw Failure($"openat '{name}' (a link or insufficient search/access permissions may prevent opening)");
        return new SafeFileHandle(descriptor, ownsHandle: true);
    }

    static SafeFileHandle OpenWindows(string path, bool writable, bool directory)
    {
        var share = writable ? 1u : 7u;
        if (directory) share = 3u; // Holding every parent without delete sharing prevents directory substitution.
        var handle = CreateFile(path, writable ? 0xc0000000u : 0x80000000u, share, 0, 3, 0x00200000u | (directory ? 0x02000000u : 0), 0);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            throw Failure("CreateFile");
        }
        try
        {
            if (!GetWindowsInformation(handle, out var information)) throw Failure("GetFileInformationByHandle");
            if ((information.Attributes & 0x400) != 0) throw new IOException($"Refusing reparse point in Direct MCP configuration: {path}");
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    static (ulong Device, ulong Inode) Identity(SafeFileHandle handle)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!GetWindowsInformation(handle, out var status)) throw Failure("GetFileInformationByHandle");
            if ((status.Attributes & (0x400 | 0x10)) != 0) throw new IOException("Direct MCP configuration is not a regular file.");
            return (status.Volume, ((ulong)status.IndexHigh << 32) | status.IndexLow);
        }

        if (OperatingSystem.IsLinux())
        {
            // The statx syscall has a fixed ABI and does not require glibc's newer fstat/statx exports.
            while (true)
            {
                var result = LinuxStatx(LinuxStatxNumber(RuntimeInformation.ProcessArchitecture), handle, string.Empty, 0x1000, 0x101, out var linux);
                if (result == 0)
                {
                    if ((linux.Mask & 0x101) != 0x101 || (linux.Mode & 0xf000) != 0x8000) throw new IOException("Direct MCP configuration is not a verifiable regular file.");
                    return (((ulong)linux.DeviceMajor << 32) | linux.DeviceMinor, linux.Inode);
                }
                if (Marshal.GetLastPInvokeError() != 4) throw Failure("statx");
            }
        }

        // Intel macOS retains a legacy fstat symbol; request its 64-bit-inode ABI explicitly.
        UnixStatus unix;
        var macResult = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => GetMacInformation(handle, out unix),
            Architecture.Arm64 => GetUnixInformation(handle, out unix),
            _ => throw new IOException("Configuration file identity inspection is unsupported on this architecture.")
        };
        if (macResult != 0) throw Failure("fstat");
        if ((unix.MacMode & 0xf000) != 0x8000) throw new IOException("Direct MCP configuration is not a regular file.");
        return (unix.MacDevice, unix.Inode);
    }

    static IOException Failure(string operation) => new($"Cannot safely open Direct MCP configuration ({operation}, native error {Marshal.GetLastPInvokeError()}); no content was written.");

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "openat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int OpenAt(int parent, string path, int flags);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fstat", SetLastError = true)]
    private static partial int GetUnixInformation(SafeFileHandle handle, out UnixStatus status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "syscall", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial long LinuxStatx(long number, SafeFileHandle handle, string path, int flags, uint mask, out LinuxStatus status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fstat$INODE64", SetLastError = true)]
    private static partial int GetMacInformation(SafeFileHandle handle, out UnixStatus status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle CreateFile(string path, uint access, uint share, nint security, uint disposition, uint flags, nint template);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "GetFileInformationByHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowsInformation(SafeFileHandle handle, out WindowsStatus status);

#pragma warning disable CS0649 // Populated by the native APIs.
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    struct UnixStatus
    {
        [FieldOffset(0)]
        public uint MacDevice;
        [FieldOffset(4)]
        public ushort MacMode;
        [FieldOffset(8)]
        public ulong Inode;
    }

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    struct LinuxStatus
    {
        [FieldOffset(0)]
        public uint Mask;
        [FieldOffset(28)]
        public ushort Mode;
        [FieldOffset(32)]
        public ulong Inode;
        [FieldOffset(136)]
        public uint DeviceMajor;
        [FieldOffset(140)]
        public uint DeviceMinor;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WindowsStatus
    {
        public uint Attributes;
        public uint CreationLow;
        public uint CreationHigh;
        public uint AccessLow;
        public uint AccessHigh;
        public uint WriteLow;
        public uint WriteHigh;
        public uint Volume;
        public uint SizeHigh;
        public uint SizeLow;
        public uint Links;
        public uint IndexHigh;
        public uint IndexLow;
    }
#pragma warning restore CS0649
}
