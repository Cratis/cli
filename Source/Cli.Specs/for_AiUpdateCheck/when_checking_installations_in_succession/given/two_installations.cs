// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_checking_installations_in_succession.given;

public class two_installations : Specification
{
    protected const string First = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    protected const string Second = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    protected string _directory = null!;
    private protected UpdateCheckCache _cache = null!;
    protected List<string> _compared = [];

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _cache = new(Path.Combine(_directory, "version-check.json"));
    }

    void Destroy()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    protected Task<AiCorpusUpdate?> Check(string installed, string? comparison) =>
        AiUpdateCheck.Check(
            _cache,
            installed,
            _ =>
            {
                _compared.Add(installed);
                return Task.FromResult(comparison);
            },
            CancellationToken.None);
}
