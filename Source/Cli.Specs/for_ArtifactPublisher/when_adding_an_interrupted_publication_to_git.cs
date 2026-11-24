// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_adding_an_interrupted_publication_to_git : given.a_git_artifact_publication
{
    const string RootIgnore = "user-owned.txt\n";
    string[] _controlFiles = null!;
    string[] _ignored = null!;
    string[] _staged = null!;

    async Task Establish()
    {
        await Publish();
        await File.WriteAllTextAsync(Path.Combine(_destination, ".gitignore"), RootIgnore);
        await File.WriteAllTextAsync(FirstSourcePath(), "modified before force");
        var interrupted = new ArtifactPublisher(new InterruptAfterJournal());
        var error = await Catch.Exception(() => interrupted.Publish(new(_plan, _destination, true), CancellationToken.None));
        error.ShouldBeOfExactType<SimulatedInterruption>();
        _controlFiles = [.. Directory.GetFiles(ArtifactPublicationStorage.ControlPath(_destination), "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(_destination, path).Replace('\\', '/'))];
    }

    async Task Because()
    {
        _ignored = await Git(["check-ignore", "--no-index", "--", .. _controlFiles]);
        await Git("add", "-A");
        _staged = await Git("ls-files");
    }

    [Fact] void should_ignore_every_control_file_including_the_ignore_file() => _ignored.ShouldEqual(_controlFiles);
    [Fact] void should_not_stage_control_files() => _staged.ShouldNotContain(path => path.StartsWith(".cratis-render/", StringComparison.Ordinal));
    [Fact] void should_keep_the_root_manifest_trackable() => _staged.ShouldContain(ArtifactPublicationStorage.ManifestFileName);
    [Fact] void should_keep_generated_artifacts_trackable() => _staged.ShouldContain(Path.GetRelativePath(_destination, FirstSourcePath()).Replace('\\', '/'));
    [Fact] void should_not_modify_the_root_ignore_file() => File.ReadAllText(Path.Combine(_destination, ".gitignore")).ShouldEqual(RootIgnore);
    [Fact] void should_have_staging_to_ignore() => _controlFiles.ShouldContain(path => path.StartsWith(".cratis-render/staging/", StringComparison.Ordinal));
    [Fact] void should_have_backups_to_ignore() => _controlFiles.ShouldContain(path => path.StartsWith(".cratis-render/backup/", StringComparison.Ordinal));
    [Fact] void should_have_a_journal_to_ignore() => _controlFiles.ShouldContain(".cratis-render/journal.json");
    [Fact] void should_have_a_self_ignoring_file() => _controlFiles.ShouldContain(".cratis-render/.gitignore");

    sealed class InterruptAfterJournal : IArtifactPublicationObserver
    {
        public void OnCheckpoint(ArtifactPublicationCheckpoint checkpoint)
        {
            if (checkpoint == ArtifactPublicationCheckpoint.JournalWritten) throw new SimulatedInterruption();
        }
    }

    sealed class SimulatedInterruption : Exception;
}
