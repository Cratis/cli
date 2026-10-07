// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Removes and verifies inherited ACLs on empty Direct MCP backup files before copying private content.</summary>
[UnsupportedOSPlatform("windows")]
internal static partial class AiUnixFileAcl
{
    /// <summary>Clears extended access entries on the opened backup and verifies their absence.</summary>
    /// <param name="handle">The empty backup's handle.</param>
    /// <exception cref="IOException">When the ACL cannot be cleared or verified.</exception>
    internal static void Clear(SafeFileHandle handle)
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                if (RemoveLinuxAttribute(handle, "system.posix_acl_access") != 0 && Marshal.GetLastPInvokeError() != 61) throw Failure();
            }
            else if (OperatingSystem.IsMacOS())
            {
                var empty = CreateMacAcl(0);
                if (empty == 0) throw Failure();
                try
                {
                    if (SetMacAcl(handle, empty) != 0) throw Failure();
                }
                finally
                {
                    _ = FreeMacAcl(empty);
                }
            }
            else
            {
                throw Failure();
            }
            if (HasEntries(handle)) throw Failure();
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new IOException("Cannot verify private Direct MCP backup ACL protection; no content was copied.", ex);
        }
    }

    /// <summary>Gets whether an opened file has extended access entries, refusing unverified results.</summary>
    /// <param name="handle">The file's handle.</param>
    /// <returns>Whether extended access entries exist.</returns>
    /// <exception cref="IOException">When ACL protection cannot be inspected.</exception>
    internal static bool HasEntries(SafeFileHandle handle)
    {
        if (OperatingSystem.IsLinux())
        {
            var length = GetLinuxAttribute(handle, "system.posix_acl_access", 0, 0);
            if (length >= 0) return true;
            if (Marshal.GetLastPInvokeError() == 61) return false; // ENODATA: no access ACL.
            throw Failure();
        }
        if (OperatingSystem.IsMacOS())
        {
            var acl = GetMacAcl(handle);
            if (acl == 0)
            {
                if (Marshal.GetLastPInvokeError() == 2) return false; // ENOENT: no extended ACL on the open file.
                throw Failure();
            }
            try
            {
                if (GetMacEntry(acl, 0, out _) == 0) return true; // ACL_FIRST_ENTRY, unlike POSIX, returns zero for an entry.
                if (Marshal.GetLastPInvokeError() == 2) return false;
                throw Failure();
            }
            finally
            {
                _ = FreeMacAcl(acl);
            }
        }
        throw Failure();
    }

    static IOException Failure() => new("Cannot establish private Direct MCP backup ACL protection; no content was copied.");

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fremovexattr", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int RemoveLinuxAttribute(SafeFileHandle handle, string name);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fgetxattr", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial nint GetLinuxAttribute(SafeFileHandle handle, string name, nint value, nuint size);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "acl_init", SetLastError = true)]
    private static partial nint CreateMacAcl(int count);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "acl_set_fd", SetLastError = true)]
    private static partial int SetMacAcl(SafeFileHandle handle, nint acl);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "acl_get_fd", SetLastError = true)]
    private static partial nint GetMacAcl(SafeFileHandle handle);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "acl_get_entry", SetLastError = true)]
    private static partial int GetMacEntry(nint acl, int entry, out nint result);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "acl_free")]
    private static partial int FreeMacAcl(nint acl);
}
