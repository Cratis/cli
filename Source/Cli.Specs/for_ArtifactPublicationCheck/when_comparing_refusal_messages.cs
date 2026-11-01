// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_comparing_refusal_messages : given.a_publication_check
{
    [Theory]
    [InlineData("modified", false)]
    [InlineData("stale", false)]
    [InlineData("stale", true)]
    [InlineData("unmanaged", false)]
    [InlineData("unmanaged", true)]
    [InlineData("directory", false)]
    [InlineData("schema", false)]
    [InlineData("identity", false)]
    [InlineData("reserved", false)]
    [InlineData("case", false)]
    [InlineData("escape", false)]
    [InlineData("non-publishable", false)]
    public async Task should_share_the_real_publication_message_and_leave_the_destination_untouched(string condition, bool force)
    {
        await Arrange(condition);
        var before = Snapshot(_destination);

        var result = await Check(force);

        result.Refused.ShouldBeGreaterThan(0);
        Snapshot(_destination).ShouldEqual(before);
        var error = await Catch.Exception(() => Publish(force));
        error.ShouldBeOfExactType<UnsafeArtifactPublication>();
        result.Receipt.Changes.First(_ => _.Kind == "refused").Reason.ShouldEqual(error.Message);
        if (condition == "modified" || condition == "stale" || condition == "unmanaged" || condition == "directory")
        {
            result.Receipt.Changes.First(_ => _.Kind == "refused").Path.ShouldNotBeNull();
        }
    }

    async Task Arrange(string condition)
    {
        if (condition == "non-publishable")
        {
            await File.WriteAllTextAsync(_file, string.Join('\n', ["concept ProjectId : Uuid", "module Projects", "  feature Registration", "    slice StateChange RegisterProject", "      command RegisterProject", "        projectId ProjectId identifier", "        produces ProjectRegistered", "          projectId = projectId", "      event ProjectRegistered", "        projectId ProjectId"]));
            _plan = (await Plan()).Artifacts!;
            _plan.Success.ShouldBeFalse();
            return;
        }

        if (condition == "unmanaged" || condition == "directory")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FirstSourcePath())!);
            if (condition == "directory")
            {
                Directory.CreateDirectory(FirstSourcePath());
            }
            else
            {
                await File.WriteAllTextAsync(FirstSourcePath(), "unmanaged bytes");
            }

            return;
        }

        await Publish();
        if (condition == "modified")
        {
            await File.WriteAllTextAsync(FirstSourcePath(), "user modified bytes");
            return;
        }

        if (condition == "stale")
        {
            AddStale("stale.cs", modified: true);
            return;
        }

        var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        manifest = condition switch
        {
            "schema" => manifest with { SchemaVersion = "99" },
            "identity" => manifest with { ApplicationName = "OtherApp" },
            "reserved" => manifest with { Artifacts = [.. manifest.Artifacts, new(".cratis-render/staging/reserved.cs", "hash")] },
            "case" => manifest with { Artifacts = [.. manifest.Artifacts, new("case.cs", "hash"), new("CASE.cs", "hash")] },
            "escape" => manifest with { Artifacts = [.. manifest.Artifacts, new("../escape.cs", "hash")] },
            _ => manifest
        };
        ArtifactPublicationStorage.WriteDurable(ArtifactPublicationStorage.ManifestPath(_destination), ArtifactPublicationStorage.Serialize(manifest));
    }
}
