// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RunCommand.when_reporting_notes.given;

public class a_notes_output : when_reporting_refusals.given.a_refusal_output
{
    protected static readonly IReadOnlyList<StageRuntimeNote> Notes =
    [
        new("PLAY0270", "Concept 'One' declares a report-only annotation.", "one.play(1,1)", JsonSerializer.SerializeToElement("Information")),
        new("PLAY0270", "Concept 'Two' declares a report-only annotation.", "two.play(2,1)", JsonSerializer.SerializeToElement(1))
    ];

    void Establish() => _status = new StageRuntimeStatus("unsupported", "semantic", [new("Plan", "BadConcept", "STAGE-ESM-005: Concept 'Bad' cannot render.")]) { Notes = Notes };
}
