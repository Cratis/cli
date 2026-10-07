// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Cli.for_DirectMcpRegistration.given;

public class a_restricted_configuration : a_home_and_a_project
{
    string? _descriptor;
    protected string _path;

    void Establish()
    {
        _path = HomeFile(".claude.json");
        Write(_path, "{\"theme\":\"dark\"}\n");
        if (OperatingSystem.IsWindows())
        {
            const AccessControlSections Sections = AccessControlSections.Access;
            using var identity = WindowsIdentity.GetCurrent();
            var security = new FileInfo(_path).GetAccessControl(Sections);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.FullControl, AccessControlType.Allow));
            new FileInfo(_path).SetAccessControl(security);
            _descriptor = new FileInfo(_path).GetAccessControl(Sections).GetSecurityDescriptorSddlForm(Sections);
        }
        else
        {
            File.SetUnixFileMode(_path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    protected void ShouldRetainPermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            const AccessControlSections Sections = AccessControlSections.Access;
            var expected = new RawSecurityDescriptor(_descriptor!);
            var actual = new RawSecurityDescriptor(new FileInfo(path).GetAccessControl(Sections).GetSecurityDescriptorSddlForm(Sections));

            // Creating a file resets Windows' inheritance provenance flag, not its ACL or protection.
            expected.SetFlags(expected.ControlFlags & ~ControlFlags.DiscretionaryAclAutoInherited);
            actual.SetFlags(actual.ControlFlags & ~ControlFlags.DiscretionaryAclAutoInherited);
            actual.GetSddlForm(Sections).ShouldEqual(expected.GetSddlForm(Sections));
        }
        else
        {
            File.GetUnixFileMode(path).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
