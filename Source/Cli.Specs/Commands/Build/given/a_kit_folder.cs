// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.given;

public class a_kit_folder : Specification
{
    public static readonly string Manifest = string.Join(
        '\n',
        "kitVersion: \"1\"",
        "name: shell-demo",
        "version: 1.0.0",
        "description: A tiny shell stack.",
        "sliceTypes: [stateChange, stateView]",
        "ui: false",
        "instructions: ./AGENTS.md",
        "skills: ./skills",
        "prompt: ./build-slice.md",
        "verify:",
        "  - test -f src/{slice}.txt",
        "  - grep -q built src/{slice}.txt",
        "git:",
        "  branch: \"build/{feature}\"",
        "  commit: \"Build {slice}\"",
        "permissions:",
        "  paths: [\"src/**\"]",
        "  commands: [ls]",
        "existingCodebase:",
        "  learn: ./learn.md",
        "  conventions: .cratis/build/conventions.md",
        "requires:",
        "  - command: git");

    protected string _kit;

    void Establish()
    {
        _kit = Path.Combine(Path.GetTempPath(), "cratis-build-specs", Guid.NewGuid().ToString("N"), "kit");
        Directory.CreateDirectory(Path.Combine(_kit, "skills", "build-slice"));
        File.WriteAllText(Path.Combine(_kit, "build-kit.yaml"), Manifest);
        File.WriteAllText(Path.Combine(_kit, "AGENTS.md"), "Each slice is src/<Slice>.txt whose line is 'built <Slice>'.");
        File.WriteAllText(Path.Combine(_kit, "build-slice.md"), "Build {slice} of {feature}; the order is at {order}. Write src/{slice}.txt.");
        File.WriteAllText(Path.Combine(_kit, "learn.md"), "Learn the conventions of {application}.");
        File.WriteAllText(Path.Combine(_kit, "skills", "build-slice", "SKILL.md"), "---\nname: build-slice\n---\nBuild a slice.");
    }

    void Destroy()
    {
        var root = Path.GetDirectoryName(_kit);
        if (root is not null && Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    protected void Write(string name, string content) => File.WriteAllText(Path.Combine(_kit, name), content);
}
