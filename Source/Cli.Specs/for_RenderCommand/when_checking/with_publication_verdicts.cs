// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_publication_verdicts : given.a_check_command
{
    [Theory]
    [InlineData("fresh", false, ExitCodes.ChangesPending, "write")]
    [InlineData("unchanged", false, ExitCodes.Success, "unchanged")]
    [InlineData("modified", false, ExitCodes.ValidationError, "refused")]
    [InlineData("modified", true, ExitCodes.ChangesPending, "write")]
    [InlineData("unmanaged", false, ExitCodes.ValidationError, "refused")]
    [InlineData("stale", false, ExitCodes.ValidationError, "refused")]
    [InlineData("recovery", false, ExitCodes.ValidationError, "refused")]
    public async Task should_return_the_verdict_exit_code_with_a_read_only_receipt(string condition, bool force, int exitCode, string kind)
    {
        await Arrange(condition);
        _settings.Force = force;
        var before = Snapshot(_destination);

        var output = await Capture();

        output.ExitCode.ShouldEqual(exitCode);
        output.Stderr.ShouldEqual(string.Empty);
        Snapshot(_destination).ShouldEqual(before);
        using var json = JsonDocument.Parse(output.Stdout);
        var root = json.RootElement;
        root.GetProperty("target").GetString().ShouldEqual("cratis");
        root.GetProperty("destination").GetString().ShouldEqual(_destination);
        root.GetProperty("publication").GetProperty("status").GetString().ShouldEqual("check");
        var changes = root.GetProperty("publication").GetProperty("changes").EnumerateArray().ToArray();
        changes.ShouldContain(_ => _.GetProperty("kind").GetString() == kind);
        if (kind == "refused")
        {
            changes.Where(_ => _.GetProperty("kind").GetString() == "refused").All(_ => _.GetProperty("reason").GetString()!.Length > 0).ShouldBeTrue();
        }

        if (condition == "fresh" || condition == "unchanged")
        {
            changes.All(_ => _.GetProperty("kind").GetString() == kind).ShouldBeTrue();
            changes.Length.ShouldEqual(_artifactPlan.Artifacts.Length);
            Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
        }

        if (condition == "recovery")
        {
            root.GetProperty("recoveryPending").GetBoolean().ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_emit_nothing_in_quiet_mode_for_changes_or_refusals(bool refusal)
    {
        await Arrange(refusal ? "unmanaged" : "fresh");
        _settings.Output = OutputFormats.Quiet;

        var output = await Capture();

        output.ExitCode.ShouldEqual(refusal ? ExitCodes.ValidationError : ExitCodes.ChangesPending);
        output.Stdout.ShouldEqual(string.Empty);
        output.Stderr.ShouldEqual(string.Empty);
    }

    async Task Arrange(string condition)
    {
        if (condition == "fresh")
        {
            return;
        }

        var path = ArtifactPublicationStorage.ArtifactPath(_destination, _artifactPlan.Artifacts[0].RelativePath);
        if (condition == "unmanaged")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "unmanaged bytes");
            return;
        }

        await RenderFirst();
        if (condition == "modified")
        {
            await File.WriteAllTextAsync(path, "user edited bytes");
        }
        else if (condition == "stale")
        {
            var stale = ArtifactPublicationStorage.ArtifactPath(_destination, "stale.cs");
            await File.WriteAllTextAsync(stale, "owned bytes");
            var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
            ArtifactPublicationStorage.WriteDurable(ArtifactPublicationStorage.ManifestPath(_destination), ArtifactPublicationStorage.Serialize(manifest with
            {
                Artifacts = [.. manifest.Artifacts, new("stale.cs", ArtifactPublicationStorage.Hash(stale))]
            }));
            await File.WriteAllTextAsync(stale, "user edited stale bytes");
        }
        else if (condition == "recovery")
        {
            ArtifactPublicationStorage.WriteDurable(ArtifactPublicationStorage.JournalPath(_destination), "interrupted journal bytes");
            ArtifactPublicationStorage.WriteDurable(ArtifactPublicationStorage.StagingPath(_destination, "pending.cs"), "staged bytes");
        }
    }
}
