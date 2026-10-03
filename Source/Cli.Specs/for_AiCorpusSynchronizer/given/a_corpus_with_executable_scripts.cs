// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class a_corpus_with_executable_scripts : Specification
{
    protected string _project = null!;
    protected string _corpus = null!;
    protected AiConfiguration _configuration = new([], [], []);
    protected Dictionary<string, string> _expected = [];
    protected SyncResult _result = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "hooks"));
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{}");
        foreach (var (extension, comment, shebang, body) in new[]
        {
            ("mjs", "//", "#!/usr/bin/env node", "console.log('ok');"),
            ("js", "//", "#!/usr/bin/env node", "console.log('ok');"),
            ("cjs", "//", "#!/usr/bin/env node", "console.log('ok');"),
            ("ts", "//", "#!/usr/bin/env tsx", "console.log('ok');"),
            ("tsx", "//", "#!/usr/bin/env tsx", "console.log('ok');"),
            ("cs", "//", "#!/usr/bin/env dotnet", "System.Console.WriteLine(\"ok\");"),
            ("sh", "#", "#!/bin/sh", "echo ok"),
            ("py", "#", "#!/usr/bin/env python3", "print('ok')"),
            ("bash", "#", "#!/usr/bin/env bash", "echo ok"),
        })
        {
            foreach (var (suffix, newline, scriptBody) in new[] { ("lf", "\n", body + "\n"), ("crlf", "\r\n", body + "\r\n"), ("only", "", "") })
            {
                var name = $"{suffix}.{extension}";
                File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "hooks", name), shebang + newline + scriptBody);
                var separator = newline.Length == 0 ? "\n" : newline;
                _expected[name] = $"{shebang}{separator}{comment} cratis-ai-managed: hooks/{name}{separator}{scriptBody}";
            }
            var plain = $"plain.{extension}";
            File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "hooks", plain), body);
            _expected[plain] = $"{comment} cratis-ai-managed: hooks/{plain}\n{body}";
        }
    }

    protected string Installed(string name) => Path.Combine(_project, ".cratis", "ai", "hooks", name);

    void Destroy()
    {
        if (Directory.Exists(_project)) Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
