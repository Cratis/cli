// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusProfiles.given;

public class a_corpus : Specification
{
    protected string _corpus = null!;

    void Establish()
    {
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai"));
    }

    protected void WriteCatalog(string json) => File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), json);

    void Destroy() => Directory.Delete(_corpus, true);
}
