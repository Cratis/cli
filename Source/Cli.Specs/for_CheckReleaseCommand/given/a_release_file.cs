// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.given;

public class a_release_file : Specification, IDisposable
{
    protected string _path;
    protected ReleaseDescription? _release;
    protected string _problem = string.Empty;
    protected bool _valid;

    public static string Valid => "{ \"release\": \"v3.2.0\", \"client\": { \"type\": \"dotnet\", \"version\": \"19.181.3\", \"protocolVersion\": 1 }, \"descriptorSet\": \"AQID\", \"eventTypes\": [ { \"type\": { \"id\": \"BookAdded\", \"generation\": 1 }, \"schema\": \"{}\" } ] }";

    void Establish() => _path = Path.Combine(Path.GetTempPath(), $"cratis-release-{Guid.NewGuid():N}.json");

    /// <inheritdoc/>
#pragma warning disable CA1033 // Interface methods should be callable by child types
    void IDisposable.Dispose()
#pragma warning restore CA1033
    {
        File.Delete(_path);
    }

    protected void Read(string content)
    {
        File.WriteAllText(_path, content);
        _valid = ReleaseReader.TryRead(_path, out _release, out _problem);
    }
}
