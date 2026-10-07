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
    /// <param name="beforeReplace">The original document's path/content precondition.</param>
    /// <param name="beforeOpen">An optional interleaving seam after the final path check.</param>
    /// <param name="write">An optional failing-write seam.</param>
    /// <exception cref="IOException">When the opened configuration cannot be verified or written safely.</exception>
    internal static void Write(string path, string content, Action? beforeReplace, Action<string>? beforeOpen, Action<Stream, string>? write)
    {
        var parents = new List<SafeFileHandle>();
        try
        {
            var parent = OpenParent(path, parents);
            using var original = Open(path, parent, writable: false);
            var expectedIdentity = Identity(original);
            var expectedBytes = Read(original);
            beforeReplace?.Invoke();
            beforeOpen?.Invoke(path);
            using var current = Open(path, parent, writable: true);
            if (Identity(current) != expectedIdentity || !Read(current).AsSpan().SequenceEqual(expectedBytes))
            {
                throw new IOException($"{path} changed during MCP installation; retry after reviewing it.");
            }
            using var stream = new FileStream(current, FileAccess.ReadWrite);
            stream.Seek(0, SeekOrigin.Begin);
            if (write is not null) write(stream, content);
            else stream.Write(Encoding.UTF8.GetBytes(content));
            stream.Flush(true);
            stream.SetLength(stream.Position);
            stream.Flush(true);
        }
        finally
        {
            foreach (var parent in parents) parent.Dispose();
        }
    }

    /// <summary>
    /// Reads a handle's full content through an explicit offset, never through the handle's shared position. A
    /// buffered <see cref="FileStream"/> can satisfy a rewind to the start of its own buffer without a real seek,
    /// which on Windows leaves the handle positioned at end-of-file for whoever reads or writes through it next -
    /// including the write that follows this verification read, which would then append instead of replace.
    /// </summary>
    /// <param name="handle">The handle to read.</param>
    /// <returns>The handle's full content.</returns>
    static byte[] Read(SafeFileHandle handle)
    {
        var bytes = new byte[RandomAccess.GetLength(handle)];
        RandomAccess.Read(handle, bytes, fileOffset: 0);
        return bytes;
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
        if (OperatingSystem.IsLinux()) flags |= 0x20000 | 0x80000 | 0x800 | (directory ? 0x10000 : 0); // NOFOLLOW, CLOEXEC, NONBLOCK, DIRECTORY.
        else if (OperatingSystem.IsMacOS()) flags |= 0x100 | 0x1000000 | 4 | (directory ? 0x100000 : 0);
        else throw new IOException("No-follow configuration writes are unsupported on this platform.");
        var descriptor = OpenAt(parent, name, flags);
        if (descriptor < 0) throw Failure("openat");
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

        // Intel macOS retains a legacy fstat symbol; request its 64-bit-inode ABI explicitly.
        var result = OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? GetMacInformation(handle, out var unix)
            : GetUnixInformation(handle, out unix);
        if (result != 0) throw Failure("fstat");
        var mode = OperatingSystem.IsMacOS() ? unix.MacMode : RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => unix.LinuxX64Mode,
            Architecture.Arm64 => unix.LinuxArm64Mode,
            _ => throw new IOException("Configuration file identity inspection is unsupported on this architecture.")
        };
        if ((mode & 0xf000) != 0x8000) throw new IOException("Direct MCP configuration is not a regular file.");
        return (OperatingSystem.IsMacOS() ? unix.MacDevice : unix.Device, unix.Inode);
    }

    static IOException Failure(string operation) => new($"Cannot safely open Direct MCP configuration ({operation}, native error {Marshal.GetLastPInvokeError()}); no content was written.");

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "openat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int OpenAt(int parent, string path, int flags);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fstat", SetLastError = true)]
    private static partial int GetUnixInformation(SafeFileHandle handle, out UnixStatus status);

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
        public ulong Device;
        [FieldOffset(0)]
        public uint MacDevice;
        [FieldOffset(4)]
        public ushort MacMode;
        [FieldOffset(8)]
        public ulong Inode;
        [FieldOffset(16)]
        public uint LinuxArm64Mode;
        [FieldOffset(24)]
        public uint LinuxX64Mode;
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
