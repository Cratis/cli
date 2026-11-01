// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_the_bundled_prologue_version : Specification
{
    string _result;

    PrologueWizardInput _input;

    void Establish() => _input = new(
        Guid.NewGuid(), [], [], null, null, new(OutputKind.Json, "", "./captures"));

    void Because() => _result = PrologueExtractorCommand.Build(_input, "/project/cratis-prologue.json");

    [Fact] void should_pin_the_image_to_the_bundled_prologue_version() => _result.EndsWith("cratis/prologue-extractor:2.0.3", StringComparison.Ordinal).ShouldBeTrue();
}
