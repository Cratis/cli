// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Cli.for_RenderFileHeader.when_resolving.given;

public class a_file_header : Specification
{
    protected const int Year = 2031;
    protected string _folder = null!;
    protected (ImmutableArray<string> Lines, string? Error, bool NotFound) _result;

    void Establish() => _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-file-header-{Guid.NewGuid():N}")).FullName;

    protected async Task Resolve(string[] values, string? file = null) =>
        _result = await RenderFileHeader.Resolve(values, file, Year, CancellationToken.None);

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
