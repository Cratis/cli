// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderBootstrap.given;

public class a_launcher : Specification
{
    protected Dictionary<string, string> _fields = new()
    {
        [StudioConnection.UrlVariable] = "https://studio.example",
        [StudioConnection.OrganizationVariable] = "org",
        [StudioConnection.BuilderIdVariable] = "id",
        [StudioConnection.BuilderTokenVariable] = "environment"
    };
    protected int _reads;
    protected string? _line = "from-pipe";
    private protected Task<StudioConnection?> Open() => BuilderBootstrap.Open(key => _fields.GetValueOrDefault(key), Read, CancellationToken.None);

    Task<string?> Read(CancellationToken cancellationToken)
    {
        _reads++;
        return Task.FromResult(_line);
    }
}
