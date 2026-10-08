// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Security.Principal;
using Cratis.Cli.Commands.Direct;

#pragma warning disable CA1416 // Facts run only on the platform their test attribute selects; Because returns early elsewhere.

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class under_a_broader_windows_inheritable_acl : given.a_home_and_a_project
{
    const string Original = "{\"theme\":\"dark\"}\n";
    string[] _originalEntries = [];
    string _path = string.Empty;

    void Establish()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var identity = WindowsIdentity.GetCurrent();
        var restricted = new DirectorySecurity();
        restricted.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        restricted.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(_project).SetAccessControl(restricted);
        var broad = new DirectoryInfo(_home).GetAccessControl();
        broad.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.Read, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(_home).SetAccessControl(broad);
        var source = ProjectFile("original.json");
        Write(source, Original);
        var access = new FileInfo(source).GetAccessControl(AccessControlSections.Access);
        access.AreAccessRulesProtected.ShouldBeFalse();
        access.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().Any(rule => rule.IsInherited).ShouldBeTrue();
        _originalEntries = Entries(source);
        _path = HomeFile(".claude.json");
        File.Move(source, _path);
        Entries(_path).ShouldContainOnly(_originalEntries);
        new FileInfo(_path).GetAccessControl().AreAccessRulesProtected.ShouldBeFalse();
    }

    void Because()
    {
        if (OperatingSystem.IsWindows()) _plan = Install(DirectMcpScope.User, ["claude"]);
    }

    [for_DesktopMcp.given.windows_only.Fact] void should_leave_one_parseable_installed_document() => IsRegistered(_path, "mcpServers").ShouldBeTrue();
    [for_DesktopMcp.given.windows_only.Fact] void should_preserve_the_original_live_access() => Entries(_path).ShouldContainOnly(_originalEntries);
    [for_DesktopMcp.given.windows_only.Fact] void should_protect_the_backup_dacl() => IsProtected(Backup()).ShouldBeTrue();
    [for_DesktopMcp.given.windows_only.Fact] void should_copy_all_effective_entries_without_directory_grants() => Entries(Backup()).ShouldContainOnly(_originalEntries);
    [for_DesktopMcp.given.windows_only.Fact] void should_convert_inherited_entries_to_explicit_entries() => HasOnlyExplicitEntries(Backup()).ShouldBeTrue();
    [for_DesktopMcp.given.windows_only.Fact] void should_back_up_the_exact_bytes() => File.ReadAllText(Backup()).ShouldEqual(Original);

    string Backup() => Directory.GetFiles(_home, ".claude.json.*.bak").Single();

    static string[] Entries(string path)
    {
        if (!OperatingSystem.IsWindows()) return [];
        return [.. new FileInfo(path).GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>()
            .Select(rule => $"{rule.IdentityReference}:{rule.FileSystemRights}:{rule.AccessControlType}").Order(StringComparer.Ordinal)];
    }

    static bool IsProtected(string path) => OperatingSystem.IsWindows() && new FileInfo(path).GetAccessControl().AreAccessRulesProtected;

    static bool HasOnlyExplicitEntries(string path) => OperatingSystem.IsWindows()
        && new FileInfo(path).GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().All(rule => !rule.IsInherited);
}
