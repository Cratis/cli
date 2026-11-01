// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.for_DirectMcpRegistration.given;

public class a_configuration_under_an_inheritable_acl : a_home_and_a_project
{
    protected string _path;
    protected string _alias;
    protected string _acl;
    protected bool _inherited;

    void Establish()
    {
        var original = ProjectFile("original.json");
        Write(original, "{\"theme\":\"dark\"}\n");
        if (OperatingSystem.IsWindows()) return;
        File.SetUnixFileMode(original, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        if (OperatingSystem.IsMacOS())
        {
            unix_acl_tools.Run("/bin/chmod", "+a", "everyone allow readattr", original);
            unix_acl_tools.Run("/bin/chmod", "+a", "everyone allow read,file_inherit,directory_inherit", _home);
        }
        else
        {
            unix_acl_tools.Run("setfacl", "-m", "u:daemon:---", original);
            unix_acl_tools.Run("setfacl", "-m", "d:u:daemon:r--,d:m::r--", _home);
        }
        _path = HomeFile(".claude.json");
        File.Move(original, _path);
        _alias = ProjectFile("original-inode.json");
        unix_acl_tools.Run("ln", _path, _alias);
        _acl = unix_acl_tools.Entries(_path);
        var probe = HomeFile("inherited-probe");
        File.WriteAllText(probe, string.Empty);
        using (var handle = File.OpenHandle(probe)) _inherited = AiUnixFileAcl.HasEntries(handle);
        File.Delete(probe);
    }

    protected static void ShouldHavePrivateBackup(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        using var handle = File.OpenHandle(path);
        AiUnixFileAcl.HasEntries(handle).ShouldBeFalse();
        File.GetUnixFileMode(handle).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
