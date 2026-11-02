// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class a_corpus_with_language_rules : Specification
{
    protected string _project = null!;
    protected string _corpus = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var rules = Path.Combine(_corpus, ".cratis", "ai", "rules");
        Directory.CreateDirectory(rules);
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\",\"typescript\",\"elixir\",\"python\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\"}],\"engineeringProfiles\":[]}");
        WriteRule(rules, "mixed.md", "**/*.cs,**/*.ts,**/*.ex,**/*.exs");
        WriteRule(rules, "python.md", "**/*.py");
        WriteRule(rules, "styles.md", "**/*.css");
        WriteRule(rules, "braced.md", "**/*.{ex,exs}");
        WriteRule(rules, "alternatives.md", "**/{*.cs,*.py}");
        WriteRule(rules, "nested.md", "**/*.{cs,{ts,py}}");
        WriteRule(rules, "groups.md", "{src,lib}/**/*.{py,go}");
        WriteRule(rules, "scripting.md", "**/*.sh,**/*.py,**/*.mjs,**/*.cjs,**/*.ps1,scripts/**,tools/**");
        WriteRule(rules, "everything.md", "**/*,**/*.py");
    }

    protected string Installed(string name) => Path.Combine(_project, ".cratis", "ai", "rules", name);

    static void WriteRule(string rules, string name, string applyTo) =>
        File.WriteAllText(Path.Combine(rules, name), $"---\napplyTo: \"{applyTo}\"\n---\n# Rule");

    void Destroy()
    {
        if (Directory.Exists(_project)) Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
