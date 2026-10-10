// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class a_corpus_with_profile_scoped_rules : Specification
{
    protected string _project = null!;
    protected string _corpus = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var rules = Path.Combine(_corpus, ".cratis", "ai", "rules");
        Directory.CreateDirectory(rules);
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[],\"profiles\":[\"cratis\",\"cratis/full\",\"cratis/example\",\"cratis/example/child\",\"cratis/example-other\"],\"languages\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis\",\"composes\":[\"cratis/example\"]},{\"id\":\"cratis/full\"},{\"id\":\"cratis/example\"},{\"id\":\"cratis/example/child\"},{\"id\":\"cratis/example-other\"}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(rules, "scoped.md"), "---\napplyTo: \"**/*\"\nprofile: cratis/example\n---\n# Scoped rule");
        File.WriteAllText(Path.Combine(rules, "general.md"), "---\napplyTo: \"**/*\"\n---\n# General rule");
    }

    protected string Installed(string name) => Path.Combine(_project, ".cratis", "ai", "rules", name);

    void Destroy()
    {
        if (Directory.Exists(_project)) Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
