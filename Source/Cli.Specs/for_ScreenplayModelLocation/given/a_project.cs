// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation.given;

public class a_project : Specification
{
    protected string _project;
    protected string _located;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), $"cratis-model-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_project);
    }

    protected string PathOf(string relative) => Path.Combine(_project, relative);

    protected void Play(string relative)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathOf(relative))!);
        File.WriteAllText(PathOf(relative), "domain Sample\n");
    }

    protected void State(string relative, bool interrupted = false)
    {
        var metadata = Path.Combine(PathOf(relative), ".screenplay");
        Directory.CreateDirectory(metadata);
        File.WriteAllText(Path.Combine(metadata, interrupted ? "pending.json" : "identities.json"), "{}");
    }

    void Destroy() => Directory.Delete(_project, recursive: true);
}
