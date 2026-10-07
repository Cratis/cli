// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Preserves the identities Unix permission bits apply to before shared configuration content is copied.</summary>
[UnsupportedOSPlatform("windows")]
internal static partial class AiUnixFileOwnership
{
    const int Interrupted = 4;
    const uint OwnershipMask = 0x18;

    /// <summary>Copies owner, group and mode between already-open files, refusing to widen access when ownership cannot be set.</summary>
    /// <param name="source">The original file's handle.</param>
    /// <param name="destination">The empty replacement's handle, initially owner-only.</param>
    internal static void Copy(SafeFileHandle source, SafeFileHandle destination)
    {
        var ownership = Read(source);
        if (Read(destination) != ownership) Set(destination, ownership.User, ownership.Group);

        // Changing ownership can clear set-id bits; apply the original mode only after ownership is established.
        File.SetUnixFileMode(destination, File.GetUnixFileMode(source));
    }

    /// <summary>Reads ownership from the open file, not a pathname that could have been replaced.</summary>
    /// <param name="handle">The open file's handle.</param>
    /// <returns>The user and group IDs.</returns>
    /// <exception cref="IOException">When ownership cannot be verified on this platform.</exception>
    internal static (uint User, uint Group) Read(SafeFileHandle handle)
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                while (true)
                {
                    var result = Statx(handle, string.Empty, 0x1000, OwnershipMask, out var status);
                    var error = Marshal.GetLastPInvokeError();
                    if (result == 0 && (status.Mask & OwnershipMask) == OwnershipMask) return (status.User, status.Group);
                    if (result != 0 && error == Interrupted) continue;
                    throw Failure("statx", error);
                }
            }
            if (OperatingSystem.IsMacOS())
            {
                var attributes = new AttributeList { BitmapCount = 5, Common = 0x18000 }; // ATTR_CMN_OWNERID | ATTR_CMN_GRPID
                while (true)
                {
                    var result = GetAttributes(handle, ref attributes, out var status, 12, 0);
                    var error = Marshal.GetLastPInvokeError();
                    if (result == 0 && status.Length == 12) return (status.User, status.Group);
                    if (result != 0 && error == Interrupted) continue;
                    throw Failure("fgetattrlist", error);
                }
            }
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new IOException("The platform API needed to preserve Unix configuration ownership is unavailable; no content was copied.", ex);
        }
        throw new IOException("Preserving Unix configuration ownership is unsupported on this platform; no content was copied.");
    }

    /// <summary>Sets ownership on the opened destination before any content is copied.</summary>
    /// <param name="handle">The destination's handle.</param>
    /// <param name="user">The original owner.</param>
    /// <param name="group">The original group.</param>
    /// <exception cref="IOException">When the original ownership cannot be assigned.</exception>
    internal static void Set(SafeFileHandle handle, uint user, uint group)
    {
        try
        {
            while (true)
            {
                var result = ChangeOwner(handle, user, group);
                var error = Marshal.GetLastPInvokeError();
                if (result == 0) return;
                if (error != Interrupted) throw Failure("fchown", error);
            }
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new IOException("The platform API needed to preserve Unix configuration ownership is unavailable; no content was copied.", ex);
        }
    }

    static IOException Failure(string operation, int error) =>
        new($"Cannot preserve Unix configuration ownership ({operation}, errno {error}); no content was copied.");

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "statx", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int Statx(SafeFileHandle handle, string path, int flags, uint mask, out LinuxStatus status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fgetattrlist", SetLastError = true)]
    private static partial int GetAttributes(SafeFileHandle handle, ref AttributeList attributes, out OwnershipResult result, nuint size, nuint options);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fchown", SetLastError = true)]
    private static partial int ChangeOwner(SafeFileHandle handle, uint user, uint group);

#pragma warning disable CS0649 // Native output fields are populated by the kernel.
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
    struct OwnershipResult
    {
        public uint Length;
        public uint User;
        public uint Group;
    }

    /// <summary>Fixed 256-byte Linux statx ABI; only its ownership fields are needed.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    struct LinuxStatus
    {
        [FieldOffset(0)]
        public uint Mask;
        [FieldOffset(20)]
        public uint User;
        [FieldOffset(24)]
        public uint Group;
    }
#pragma warning restore CS0649
}
