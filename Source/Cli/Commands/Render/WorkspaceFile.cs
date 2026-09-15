// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Opens once and admits only the object identified by that handle. Nonblocking Unix open
/// prevents a FIFO with no writer from hanging before admission. This is not a deadline
/// for remote filesystems or device drivers that ignore nonblocking operation.
/// </summary>
internal static partial class WorkspaceFile
{
    const int Interrupted = 4;
    const uint StatxType = 1;
    const int AtEmptyPath = 0x1000;
    const uint RegularVnode = 1;
    static readonly HashSet<string> _deviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$", "CLOCK$", "GLOBALROOT"
    };

    internal static SafeFileHandle Open(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(path) || path.Contains('\0'))
        {
            throw new UnsafeWorkspaceInput("Workspace input requires a nonempty path without NUL characters.");
        }

        SafeFileHandle handle;
        if (OperatingSystem.IsWindows())
        {
            ValidateWindowsPath(path);
            handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            // O_RDONLY (0) | O_NONBLOCK | O_CLOEXEC | O_NOCTTY, from each platform's fcntl.h.
            var flags = OperatingSystem.IsLinux() ? 0x800 | 0x80000 | 0x100 : 0x4 | 0x1000000 | 0x20000;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var descriptor = OpenUnix(path, flags);
                var error = Marshal.GetLastPInvokeError();
                if (descriptor >= 0)
                {
                    // Zero is a valid descriptor; ownership starts before any validation can throw.
                    handle = new SafeFileHandle(descriptor, ownsHandle: true);
                    break;
                }

                if (error != Interrupted)
                {
                    throw OpenFailure(path, error);
                }
            }
        }
        else
        {
            throw new UnsafeWorkspaceInput("Regular workspace file admission is unsupported on this platform.");
        }

        try
        {
            Validate(handle, cancellationToken);
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Validates the opened object, never its name; the caller retains ownership.
    /// </summary>
    /// <param name="handle">The open input handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="UnsafeWorkspaceInput">The handle is not provably a regular file.</exception>
    internal static void Validate(SafeFileHandle handle, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (OperatingSystem.IsWindows())
            {
                if (GetFileType(handle) != 1)
                {
                    throw new UnsafeWorkspaceInput("Workspace input must be a regular disk file.");
                }

                var attributes = File.GetAttributes(handle);
                if (attributes.HasFlag(FileAttributes.Directory) || attributes.HasFlag(FileAttributes.Device))
                {
                    throw new UnsafeWorkspaceInput("Workspace input must be a regular disk file.");
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = Statx(handle, string.Empty, AtEmptyPath, StatxType, out var status);
                    var error = Marshal.GetLastPInvokeError();
                    if (result == 0)
                    {
                        if ((status.Mask & StatxType) == 0 || (status.Mode & 0xf000) != 0x8000)
                        {
                            throw new UnsafeWorkspaceInput("Workspace input must be a regular file (statx type).");
                        }

                        break;
                    }

                    if (error != Interrupted)
                    {
                        throw InspectionFailure("statx", error);
                    }
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                var attributes = new AttributeList { BitmapCount = 5, Common = 0x8 }; // ATTR_CMN_OBJTYPE
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = GetAttributes(handle, ref attributes, out var status, 8, 0);
                    var error = Marshal.GetLastPInvokeError();
                    if (result == 0)
                    {
                        if (status.Length != 8 || status.ObjectType != RegularVnode)
                        {
                            throw new UnsafeWorkspaceInput("Workspace input must be a regular file (vnode type).");
                        }

                        break;
                    }

                    if (error != Interrupted)
                    {
                        throw InspectionFailure("fgetattrlist", error);
                    }
                }
            }
            else
            {
                throw new UnsafeWorkspaceInput("Regular workspace file admission is unsupported on this platform.");
            }
        }
        catch (Exception exception) when (exception is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new UnsafeWorkspaceInput("The platform API required to verify a regular workspace file is unavailable.", exception);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    internal static void ValidateWindowsPath(string path)
    {
        var normalized = path.Replace('/', '\\');
        if (normalized.StartsWith(@"\\?\", StringComparison.Ordinal) ||
            normalized.StartsWith(@"\\.\", StringComparison.Ordinal) ||
            normalized.StartsWith(@"\??\", StringComparison.Ordinal))
        {
            throw new UnsafeWorkspaceInput("Workspace input cannot use a device or NT namespace path.");
        }

        // DOS device names remain special even with an extension or in a disk directory.
        // Reject alternate data streams as well; only the drive designator may contain a colon.
        var fullPath = Path.GetFullPath(normalized);
        var segments = fullPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            if (index == 0 && segment.Length == 2 && char.IsAsciiLetter(segment[0]) && segment[1] == ':')
            {
                continue;
            }

            var name = segment.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
            if (segment.Contains(':') || _deviceNames.Contains(name) ||
                (name.Length == 4 && (name.StartsWith("COM", StringComparison.Ordinal) || name.StartsWith("LPT", StringComparison.Ordinal)) && "0123456789¹²³".Contains(name[3])))
            {
                throw new UnsafeWorkspaceInput("Workspace input cannot use a device name or alternate data stream.");
            }
        }
    }

    static Exception OpenFailure(string path, int error) => error switch
    {
        // Preserve RenderCommand's existing missing-file diagnostic without another pathname lookup.
        2 => new FileNotFoundException("Workspace input file was not found.", path),
        20 => new DirectoryNotFoundException("A workspace input path component is not a directory."),
        _ => new UnsafeWorkspaceInput($"Workspace input could not be opened (errno {error}).", new Win32Exception(error))
    };

    static UnsafeWorkspaceInput InspectionFailure(string operation, int error) =>
        new($"Cannot verify a regular workspace file: {operation} failed (errno {error}); no pathname fallback is permitted.", new Win32Exception(error));

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int OpenUnix(string path, int flags);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "statx", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int Statx(SafeFileHandle handle, string path, int flags, uint mask, out LinuxStatus status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fgetattrlist", SetLastError = true)]
    private static partial int GetAttributes(SafeFileHandle handle, ref AttributeList attributes, out ObjectTypeResult result, nuint size, nuint options);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint GetFileType(SafeFileHandle handle);

    // Native output fields are populated by the kernel, not managed assignments.
#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)]
    struct AttributeList
    {
        public ushort BitmapCount;
        public ushort Reserved;
        public uint Common;
        public uint Volume;
        public uint Directory;
        public uint File;
        public uint Fork;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ObjectTypeResult
    {
        public uint Length;
        public uint ObjectType;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LinuxTimestamp
    {
        public long Seconds;
        public uint Nanoseconds;
        public int Reserved;
    }

    /// <summary>
    /// Fixed 256-byte Linux statx ABI; later kernel fields occupy the uninterpreted reserved tail.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 256)]
    struct LinuxStatus
    {
        public uint Mask;
        public uint BlockSize;
        public ulong Attributes;
        public uint LinkCount;
        public uint UserId;
        public uint GroupId;
        public ushort Mode;
        public ushort Spare;
        public ulong Inode;
        public ulong Size;
        public ulong Blocks;
        public ulong AttributesMask;
        public LinuxTimestamp AccessTime;
        public LinuxTimestamp BirthTime;
        public LinuxTimestamp ChangeTime;
        public LinuxTimestamp ModificationTime;
        public uint DeviceTypeMajor;
        public uint DeviceTypeMinor;
        public uint DeviceMajor;
        public uint DeviceMinor;
        public ulong MountId;
        public uint DirectIoMemoryAlignment;
        public uint DirectIoOffsetAlignment;
        public ReservedWords Reserved;
    }

    [InlineArray(12)]
    struct ReservedWords
    {
        public ulong Element;
    }
#pragma warning restore CS0649
}
