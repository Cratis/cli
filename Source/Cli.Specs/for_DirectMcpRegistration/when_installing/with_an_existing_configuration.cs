// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Text;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_an_existing_configuration : given.a_home_and_a_project
{
    byte[] _original;
    string _backup;

    void Establish()
    {
        _original = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("{\r\n  // café\r\n  \"theme\": \"dark\"\r\n}\r\n")];
        File.WriteAllBytes(HomeFile(".claude.json"), _original);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(HomeFile(".claude.json"), UnixFileMode.UserRead | UnixFileMode.UserWrite);
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
            new FileInfo(_backup).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access).ShouldEqual(new FileInfo(HomeFile(".claude.json")).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access));
        }
        else
        {
            File.GetUnixFileMode(_backup).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
