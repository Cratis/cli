// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.given;

public class a_cache : Specification
{
    protected const string Key = "Cratis.Cli";
    protected string _directory = null!;
    protected string _path = null!;
    private protected UpdateCheckCache _cache = null!;
    protected int _fetches;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "version-check.json");
        _cache = new(_path);
    }

    void Destroy()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    protected Task<string?> Check(Func<CancellationToken, Task<string?>> fetch, string currentVersion = "3.18.0")
    {
        Task<string?> Counted(CancellationToken token)
        {
            _fetches++;
            return fetch(token);
        }

        return CachedVersionCheck.Check(_cache, Key, currentVersion, false, Counted, UpdateChecker.IsNewer, CancellationToken.None);
    }
}
