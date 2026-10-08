// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_an_existing_configuration : given.a_home_and_a_project
{
    byte[] _original;
    string _backup;
    string _originalAccess = string.Empty;

    void Establish()
    {
        _original = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("{\r\n  // café\r\n  \"theme\": \"dark\"\r\n}\r\n")];
        File.WriteAllBytes(HomeFile(".claude.json"), _original);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(HomeFile(".claude.json"), UnixFileMode.UserRead | UnixFileMode.UserWrite);
        else _originalAccess = new FileInfo(HomeFile(".claude.json")).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access);
    }

    void Because()
    {
        _plan = Install(DirectMcpScope.User, ["claude"]);
        _backup = Directory.GetFiles(_home, ".claude.json.*.bak").Single();
    }

    [Fact] void should_preserve_the_exact_original_bytes() => File.ReadAllBytes(_backup).ShouldEqual(_original);
    [Fact] void should_install_the_registration() => File.ReadAllText(HomeFile(".claude.json")).ShouldContain("cratis-direct");
    [Fact] void should_preserve_the_permissions()
    {
        if (OperatingSystem.IsWindows())
        {
            Entries(_backup).ShouldContainOnly(Entries(HomeFile(".claude.json")));
            new FileInfo(_backup).GetAccessControl().AreAccessRulesProtected.ShouldBeTrue();
            new FileInfo(_backup).GetAccessControl().GetAccessRules(includeExplicit: false, includeInherited: true, typeof(SecurityIdentifier)).Count.ShouldEqual(0);
            new FileInfo(HomeFile(".claude.json")).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access).ShouldEqual(_originalAccess);
        }
        else
        {
            File.GetUnixFileMode(_backup).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    [SupportedOSPlatform("windows")]
    static string[] Entries(string path) => [.. new FileInfo(path).GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>()
        .Select(rule => $"{rule.IdentityReference}:{rule.FileSystemRights}:{rule.AccessControlType}:{rule.InheritanceFlags}:{rule.PropagationFlags}").Order(StringComparer.Ordinal)];
}
