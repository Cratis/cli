// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_WorkspaceFile;

public class when_reading_at_explicit_offsets : Specification
{
    string _path;
    byte[] _first = new byte[3];
    byte[] _second = new byte[3];
    bool _isAsync;

    void Establish()
    {
        _path = Path.Combine(Path.GetTempPath(), $"cratis-workspace-{Guid.NewGuid():N}.json");
        File.WriteAllText(_path, "abcdef");
    }

    void Because()
    {
        using var handle = WorkspaceFile.Open(_path, CancellationToken.None);
        _isAsync = handle.IsAsync;
        RandomAccess.Read(handle, _first, 3);
        RandomAccess.Read(handle, _second, 0);
    }

    [Fact] void should_read_from_the_requested_offset() => Encoding.UTF8.GetString(_first).ShouldEqual("def");
    [Fact] void should_allow_reading_an_earlier_offset() => Encoding.UTF8.GetString(_second).ShouldEqual("abc");
    [Fact] void should_use_the_platform_io_mode() => _isAsync.ShouldEqual(OperatingSystem.IsWindows());

    void Destroy() => File.Delete(_path);
}
