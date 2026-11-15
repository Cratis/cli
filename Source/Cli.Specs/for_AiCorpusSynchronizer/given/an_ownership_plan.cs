// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class an_ownership_plan : a_pi_corpus
{
    protected readonly List<(Exception? Error, int Accesses)> _outcomes = [];
    protected readonly List<(string Kind, string Path)> _accesses = [];
    protected string ManifestPath => Path.Combine(_project, ".cratis", "ai.manifest.json");
    protected string _originalManifest = null!;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
        _originalManifest = File.ReadAllText(ManifestPath);
    }

    protected static void DeleteLink(string path)
    {
        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory)) Directory.Delete(path);
        else File.Delete(path);
    }

    protected void SetManifest(string files, string integrations) => File.WriteAllText(ManifestPath,
        $$"""{"SourceRevision":"test","Files":{{files}},"Integrations":{{integrations}}}""");

    protected void ExerciseAllCommands()
    {
        for (var operation = 0; operation < 9; operation++)
        {
            _accesses.Clear();
            var operations = new AiFileOperations(operation is 3 or 4 or 7 or 8)
            {
                ObserveDestination = (kind, path) => _accesses.Add((kind, path))
            };
            var error = Catch.Exception(() => Execute(operation, operations));
            _outcomes.Add((error, _accesses.Count));
        }
    }

    protected void Execute(int operation, AiFileOperations operations)
    {
        if (operation == 0) AiCorpusSynchronizer.Status(_project, operations);
        else if (operation < 5) AiCorpusSynchronizer.Uninstall(_project, operation is 2 or 4, operations.DryRun, operations);
        else AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration, new(operation is 6 or 8, operations));
    }

    protected string FileEntry(string destination) => JsonSerializer.Serialize(new AiManagedFile("rules/general.md", destination, "hash"));

    protected string LinkEntry(string? path, string? target, bool directory = true) => JsonSerializer.Serialize(new { Path = path, Target = target, IsDirectory = directory });
}
