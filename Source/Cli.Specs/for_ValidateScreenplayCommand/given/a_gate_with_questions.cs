// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

public class a_gate_with_questions : a_validation_gate_cli_process
{
    void Establish()
    {
        // Model notes refuse symlink ancestors; macOS exposes its temp directory through /var.
        _root = WithoutLinks(_root);
        _document = Path.Combine(_root, Path.GetFileName(_document));
        _gatePath = Path.Combine(_root, "screenplay.json");
        var notes = new ModelNotesDocument(ApplicationIdentity.Create("Model").ToString(),
        [
            new ModelNote(ModelNotesStore.NewId(), "question", "Who may record?", new(Application: true), null, "team", DateTimeOffset.UtcNow, [], null)
        ]);
        var state = Directory.CreateDirectory(Path.Combine(_root, ".screenplay")).FullName;
        File.WriteAllBytes(Path.Combine(state, ModelNotesStore.FileName), ModelNotesStore.Serialize(notes));
        var child = Directory.CreateDirectory(Path.Combine(_root, "child")).FullName;
        File.Move(_document, Path.Combine(child, "child.play"));
        _document = Path.Combine(child, "child.play");
        Declare("{\"validation\":{\"openQuestions\":\"fail\",\"warningsAsErrors\":false}}");
    }

    static string WithoutLinks(string path)
    {
        var directory = new DirectoryInfo(path);
        if (directory.Parent is null) return directory.FullName;
        var resolved = Path.Combine(WithoutLinks(directory.Parent.FullName), directory.Name);
        return new DirectoryInfo(resolved).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? resolved;
    }
}
