// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Resolves a deliberately selected Direct MCP scope root with native realpath semantics.
/// Components beneath this canonical root remain subject to no-follow checks.
/// </summary>
internal static partial class AiPhysicalRoot
{
    /// <summary>Canonicalizes the existing selected directory, including ancestors of link targets.</summary>
    /// <param name="path">The deliberately selected scope root.</param>
    /// <returns>The physical root.</returns>
    /// <exception cref="IOException">When native canonicalization is unavailable.</exception>
    /// <exception cref="AiMcpConfigurationInvalid">When the directory cannot be resolved.</exception>
    internal static string Resolve(string path)
    {
        if (OperatingSystem.IsWindows()) return AiProjectPaths.PhysicalRoot(path);
        try
        {
            var result = RealPath(Path.GetFullPath(path), 0);
            if (result == 0) throw new AiMcpConfigurationInvalid($"Cannot resolve Direct MCP scope directory '{path}' (realpath, errno {Marshal.GetLastPInvokeError()}); check directory existence and search permissions.");
            try
            {
                var root = Marshal.PtrToStringUTF8(result);
                if (!Directory.Exists(root)) throw new AiMcpConfigurationInvalid($"Project directory does not exist: {root}");
                return root;
            }
            finally
            {
                Free(result);
            }
        }
        catch (Exception error) when (error is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new IOException("The platform API needed to resolve the Direct MCP scope directory is unavailable; nothing was written.", error);
        }
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "realpath", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial nint RealPath(string path, nint resolved);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "free")]
    private static partial void Free(nint pointer);
}
