// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.for_DirectMcpRegistration.given;

public class a_configuration_with_another_group : a_home_and_a_project
{
    protected string _path;
    protected uint _creationGroup;
    protected uint _originalGroup;
    protected uint _originalOwner;

    void Establish()
    {
        if (OperatingSystem.IsWindows()) return;
        _path = HomeFile(".claude.json");
        Write(_path, "{\"theme\":\"dark\"}\n");
        using var handle = File.OpenHandle(_path, FileMode.Open, FileAccess.ReadWrite);
        (_originalOwner, _creationGroup) = AiUnixFileOwnership.Read(handle);
        _originalGroup = unix_with_multiple_groups.Groups().Where(group => group != _creationGroup).Max();
        AiUnixFileOwnership.Set(handle, _originalOwner, _originalGroup);
        File.SetUnixFileMode(handle, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
    }

    protected void ShouldKeepOwnership(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        using var handle = File.OpenHandle(path);
        AiUnixFileOwnership.Read(handle).ShouldEqual((_originalOwner, _originalGroup));
        File.GetUnixFileMode(handle).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
    }
}
