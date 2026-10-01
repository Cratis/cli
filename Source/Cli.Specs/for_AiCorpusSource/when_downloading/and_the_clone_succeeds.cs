// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_AiCorpusSource.when_downloading;

public class and_the_clone_succeeds : given.a_temporary_root
{
    string _repository;
    string _destination;

    void Establish()
    {
        _repository = Path.Combine(_root, "repository");
        Directory.CreateDirectory(_repository);
        File.WriteAllText(Path.Combine(_repository, "corpus.txt"), "corpus");
        Git(_repository, "init", "--quiet");
        Git(_repository, "-c", "user.name=spec", "-c", "user.email=spec@example.com", "add", ".");
        Git(_repository, "-c", "user.name=spec", "-c", "user.email=spec@example.com", "commit", "--quiet", "-m", "corpus");
    }

    void Because() => _destination = AiCorpusSource.Download(new Uri(_repository).AbsoluteUri, Path.Combine(_root, "clones"));

    [Fact] void should_check_the_corpus_out_under_the_given_root() => File.ReadAllText(Path.Combine(_destination, "corpus.txt")).ShouldEqual("corpus");

    static void Git(string workingDirectory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = workingDirectory, UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        process.WaitForExit();
        process.ExitCode.ShouldEqual(0);
    }
}
