// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class a_corpus_with_script_permissions : Specification
{
    protected const UnixFileMode ReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
    protected const UnixFileMode ExecuteBits = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
    protected string _project = null!;
    protected string _corpus = null!;
    protected SyncResult _result = null!;
    protected AiConfiguration _configuration = new([], [], []);

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var root = Path.Combine(_corpus, ".cratis", "ai");
        Directory.CreateDirectory(Path.Combine(root, "hooks", "scripts"));
        File.WriteAllText(Path.Combine(root, "manifest.json"), "{}");
        File.WriteAllText(Path.Combine(root, "profile-catalog.json"), "{}");
        WriteSource("executable.sh", "echo ok\n", ReadWrite | ExecuteBits);
        WriteSource("shebang.mjs", "#!/usr/bin/env node\nconsole.log('ok');\n", ReadWrite);
        WriteSource("library.mjs", "export const value = 1;\n", ReadWrite);
    }

    protected string Installed(string name) => Path.Combine(_project, ".cratis", "ai", "hooks", "scripts", name);

    void WriteSource(string name, string content, UnixFileMode mode)
    {
        var path = Path.Combine(_corpus, ".cratis", "ai", "hooks", "scripts", name);
        File.WriteAllText(path, content);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, mode);
    }

    void Destroy()
    {
        if (Directory.Exists(_project)) Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
