// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_checking_a_changed_model : given.a_publication_check
{
    ArtifactPublicationResult _published = null!;
    string _copy = null!;

    async Task Establish()
    {
        await Publish();
        AddStale("stale.cs", modified: false);
        _copy = Path.Combine(_folder, "copy");
        Directory.CreateDirectory(_copy);
        foreach (var directory in Directory.EnumerateDirectories(_destination, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(_copy, Path.GetRelativePath(_destination, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(_destination, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(_copy, Path.GetRelativePath(_destination, file)));
        }

        await File.WriteAllTextAsync(_file, Source.Replace("Screenplay", "Updated project", StringComparison.Ordinal));
        _plan = (await Plan()).Artifacts!;
        _before = Snapshot(_destination);
        _published = await _publisher.Publish(new(_plan, _copy, false), CancellationToken.None);
    }

    async Task Because() => _result = await Check();

    [Fact] void should_exercise_actual_writes() => _result.Written.ShouldBeGreaterThan(0);
    [Fact] void should_report_exactly_the_real_publication_changes() => _result.Receipt.Changes.Where(_ => _.Kind != "unchanged").Select(_ => new ArtifactPublicationChange(_.Path!, _.Kind, _.BeforeSha256, _.AfterSha256)).ShouldEqual(_published.Receipt.Changes);
    [Fact] void should_report_the_same_write_count() => _result.Written.ShouldEqual(_published.Written);
    [Fact] void should_report_the_same_deletion_count() => _result.Removed.ShouldEqual(_published.Removed);
    [Fact] void should_report_the_same_unchanged_count() => _result.Unchanged.ShouldEqual(_published.Unchanged);
    [Fact] void should_report_the_proposed_manifest_hash() => _result.Receipt.Manifest.ShouldEqual(_published.Receipt.Manifest);
    [Fact] void should_leave_every_file_and_directory_unchanged() => Snapshot(_destination).ShouldEqual(_before);
}
