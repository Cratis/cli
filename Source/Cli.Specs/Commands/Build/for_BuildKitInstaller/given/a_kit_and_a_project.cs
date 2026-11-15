// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildKitInstaller.given;

public class a_kit_and_a_project : Build.given.a_kit_folder
{
    public const string Checksum = "0f1e2d3c4b5a69788796a5b4c3d2e1f00f1e2d3c4b5a69788796a5b4c3d2e1f0";

    protected string _project;
    protected List<string> _log;

    protected JsonObject Record => (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(_project, BuildProjectSettings.File)))!;

    void Establish()
    {
        _project = Path.Combine(Path.GetDirectoryName(_kit)!, "project");
        Directory.CreateDirectory(_project);
        _log = [];
        Write(Path.Combine("skills", "build-slice", "SKILL.md"), "---\nname: build-slice\ndescription: Builds one slice of the shell stack.\n---\nBuild a slice.");
    }

    protected Task<BuildProjectSettings> Install(string? agent, bool existing = false) =>
        new BuildKitInstaller(_log.Add).Install(new(BuildKit.Load(_kit), Checksum, null, null), _project, agent, existing, CancellationToken.None);

    protected string Read(string path) => File.ReadAllText(Path.Combine(_project, path));
}
