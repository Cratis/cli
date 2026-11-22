// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_preparing_the_capture_directory;

public class with_receiver_output : Specification
{
    string _result;

    void Because() => _result = PrologueExtractorCommand.PrepareCaptureDirectory(
        new PrologueWizardInput(Guid.NewGuid(), [], [], null, null, new(OutputKind.Api, "http://localhost:5005", "")),
        "/project/cratis-prologue.json");

    [Fact] void should_not_prepare_a_directory() => _result.ShouldBeNull();
}
