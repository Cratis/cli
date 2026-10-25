// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai.Testing;

internal sealed class AiTestWorkspace : IDisposable
{
    internal string Root { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cratis-ai-test", Guid.NewGuid().ToString("N"));
    internal string Path => System.IO.Path.Combine(Root, "workspace");
    internal string Output => System.IO.Path.Combine(Root, "output");
    internal bool KeepTranscripts { get; set; }

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        if (!KeepTranscripts && Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
    }

    internal void Prepare(string fixtureDirectory)
    {
        Directory.CreateDirectory(Path);
        var input = System.IO.Path.Combine(fixtureDirectory, "input");
        if (Directory.Exists(input)) Copy(input, Path);
    }

    static void Copy(string source, string target)
    {
        if (File.GetAttributes(source).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("Fixture input cannot contain symbolic links.");
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            if (File.GetAttributes(file).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("Fixture input cannot contain symbolic links.");
            File.Copy(file, System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)));
        }
        foreach (var directory in Directory.GetDirectories(source)) Copy(directory, System.IO.Path.Combine(target, System.IO.Path.GetFileName(directory)));
    }
}
