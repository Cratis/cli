// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderProfileVersion.given;

/// <summary>
/// A temporary publication destination that can hold an ownership manifest and a managed Directory.Build.props.
/// </summary>
public class a_rendered_destination : Specification, IDisposable
{
    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    protected string _destination = null!;

    void Establish() => _destination = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-profile-version-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        Directory.Delete(_destination, recursive: true);
        GC.SuppressFinalize(this);
    }

    protected void RenderedWithTargetFramework(string? targetFramework)
    {
        var artifacts = new List<ManagedArtifact>();
        if (targetFramework is not null)
        {
            File.WriteAllText(Path.Combine(_destination, "Directory.Build.props"), $"<Project><PropertyGroup><TargetFramework>{targetFramework}</TargetFramework></PropertyGroup></Project>");
            artifacts.Add(new("Directory.Build.props", string.Empty));
        }

        WriteManifest(artifacts);
    }

    protected void ListedArtifact(string path) => WriteManifest([new(path, string.Empty)]);

    void WriteManifest(IReadOnlyList<ManagedArtifact> artifacts)
    {
        var manifest = new ArtifactManifest(ArtifactManifest.CurrentSchemaVersion, "1", "revision", "cratis", "1", "renderer", "1", "Orders", artifacts);
        File.WriteAllText(Path.Combine(_destination, ArtifactPublicationStorage.ManifestFileName), JsonSerializer.Serialize(manifest, _json));
    }
}
