// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

public class a_model_with_questions : for_ScreenplayReleaseBaseline.given.a_release_model
{
    protected string _notesPath;

    void Establish()
    {
        _notesPath = Path.Combine(_root, ".screenplay", ModelNotesStore.FileName);
        var notes = new ModelNotesDocument(ApplicationIdentity.Create("Model").ToString(),
        [
            new ModelNote(ModelNotesStore.NewId(), "question", "Who may record?", new(Application: true), null, "team", DateTimeOffset.UtcNow, [], null)
        ]);
        File.WriteAllBytes(_notesPath, ModelNotesStore.Serialize(notes));
    }
}
