// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Cli.for_DirectMcpRegistration.given;

public static partial class unix_with_multiple_groups
{
    internal static unsafe IReadOnlyList<uint> Groups()
    {
        var count = GetGroups(0, null);
        if (count < 0) throw new IOException("Cannot enumerate the test user's Unix groups.");
        var groups = new uint[count];
        fixed (uint* buffer = groups)
        {
            if (GetGroups(count, buffer) < 0) throw new IOException("Cannot enumerate the test user's Unix groups.");
        }
        return [.. groups.Append(GetEffectiveGroup()).Distinct()];
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "getgroups", SetLastError = true)]
    private static unsafe partial int GetGroups(int count, uint* groups);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "getegid")]
    private static partial uint GetEffectiveGroup();

    public sealed class FactAttribute : Xunit.FactAttribute
    {
        public FactAttribute()
        {
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) Skip = "Requires Unix ownership support.";
            else if (Groups().Count < 2) Skip = "The test user has only one Unix group.";
        }
    }
}
