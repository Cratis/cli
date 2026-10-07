// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Removes and verifies inherited ACLs on empty Direct MCP files before copying private content.</summary>
[UnsupportedOSPlatform("windows")]
internal static partial class AiUnixFileAcl
{
    /// <summary>Clears extended access entries on the opened file and verifies their absence.</summary>
    /// <param name="handle">The empty file's handle.</param>
    /// <param name="purpose">Whether protection is for a backup or a new client file.</param>
    /// <exception cref="IOException">When the ACL cannot be cleared or verified.</exception>
    internal static void Clear(SafeFileHandle handle, string purpose = "backup")
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                CheckLinuxResult(RemoveLinuxAttribute(handle, "system.posix_acl_access"), Marshal.GetLastPInvokeError(), purpose);
            }
            else if (OperatingSystem.IsMacOS())
            {
                var empty = CreateMacAcl(0);
                if (empty == 0) throw Failure(purpose);
                try
                {
                    if (SetMacAcl(handle, empty) != 0) throw Failure(purpose);
                }
                finally
                {
                    _ = FreeMacAcl(empty);
                }
            }
            else
            {
                throw Failure(purpose);
            }
            if (HasEntries(handle, purpose)) throw Failure(purpose);
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new IOException($"Cannot verify private Direct MCP {purpose} ACL protection; the platform API is unavailable. Create the client configuration manually or use a filesystem with ACL support; no content was copied.", ex);
        }
    }

    /// <summary>Gets whether an opened file has extended access entries, refusing unverified results.</summary>
    /// <param name="handle">The file's handle.</param>
    /// <param name="purpose">The file whose protection is being inspected.</param>
    /// <returns>Whether extended access entries exist.</returns>
    /// <exception cref="IOException">When ACL protection cannot be inspected.</exception>
    internal static bool HasEntries(SafeFileHandle handle, string purpose = "backup")
    {
        if (OperatingSystem.IsLinux())
        {
            var length = GetLinuxAttribute(handle, "system.posix_acl_access", 0, 0);
            if (length >= 0) return true;
            CheckLinuxResult(length, Marshal.GetLastPInvokeError(), purpose);
            return false; // ENODATA: no access ACL.
        }
        if (OperatingSystem.IsMacOS())
        {
            var acl = GetMacAcl(handle);
            if (acl == 0)
            {
                if (Marshal.GetLastPInvokeError() == 2) return false; // ENOENT: no extended ACL on the open file.
                throw Failure(purpose);
            }
            try
            {
                if (GetMacEntry(acl, 0, out _) == 0) return true; // ACL_FIRST_ENTRY, unlike POSIX, returns zero for an entry.
                if (Marshal.GetLastPInvokeError() == 2) return false;
                throw Failure(purpose);
            }
            finally
            {
                _ = FreeMacAcl(acl);
            }
        }
        throw Failure(purpose);
    }

    /// <summary>Distinguishes an absent POSIX ACL from a filesystem that cannot inspect it safely.</summary>
    /// <param name="result">The native operation's result.</param>
    /// <param name="error">The operation's errno.</param>
    /// <param name="purpose">The file whose protection is being inspected.</param>
    /// <exception cref="IOException">When ACL inspection is unsupported or fails.</exception>
    internal static void CheckLinuxResult(nint result, int error, string purpose)
    {
        if (result >= 0 || error == 61) return;
        if (error == 95)
        {
            throw new IOException($"Cannot verify private Direct MCP {purpose} ACL protection (errno 95: filesystem does not support POSIX ACL inspection). Create the client configuration manually or move it to a filesystem with ACL support; no content was copied.");
        }
        throw Failure(purpose, error);
    }

    static IOException Failure(string purpose, int? error = null) => new($"Cannot establish private Direct MCP {purpose} ACL protection (errno {error ?? Marshal.GetLastPInvokeError()}). Create the client configuration manually or use a filesystem with ACL support; no content was copied.");

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
