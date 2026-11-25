// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.Fixtures;

/// <summary>
/// A real git repository in a temporary folder.
/// </summary>
public sealed class TemporaryRepository : IDisposable
{
    public TemporaryRepository()
    {
        Root = Directory.CreateTempSubdirectory("affected-specs-").FullName;
        Git("init", "-q", "-b", "main");
        Git("config", "user.email", "specs@example.com");
        Git("config", "user.name", "Specs");
        Git("config", "commit.gpgsign", "false");
        Repository = new GitRepository(Root);
    }

    public string Root { get; }

    public GitRepository Repository { get; }

    public void Write(string path, string content)
    {
        var full = Path.Combine(Root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    public void Delete(string path) => File.Delete(Path.Combine(Root, path));

    public void Move(string from, string to)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Root, to))!);
        File.Move(Path.Combine(Root, from), Path.Combine(Root, to));
    }

    public string Commit(string message)
    {
        Git("add", "-A");
        Git("commit", "-q", "--allow-empty", "-m", message);
        return Git("rev-parse", "HEAD").Trim();
    }

    public string Git(params string[] arguments)
    {
        var result = ProcessRunner.Run("git", arguments, Root);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', arguments)}: {result.Error}");
        }

        return result.Output;
    }

    public void Dispose()
    {
        foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(Root, recursive: true);
    }
}
