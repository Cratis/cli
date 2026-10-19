// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesCommand.given;

public class three_releases : Specification
{
    protected IReadOnlyList<ReleaseNote> _releases = [new("3.86.0", "c"), new("3.85.0", "b"), new("3.84.0", "a")];
    protected ReleaseNotesSettings _settings = new();
    protected (ReleaseNotesPresentation Presentation, IReadOnlyList<ReleaseNote> Notes) _result;
}
