// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayValidation;

public class when_validating_an_esm_v9_model : Specification
{
    string _folder = null!;
    ValidatedScreenplay _result = null!;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        File.WriteAllBytes(Path.Combine(_folder, "PublicEvents.play"), [.. PublicEventsCorpus.V9.SourceForms.Single().Documents.Single().Bytes]);
    }

    void Because() => _result = new ScreenplayValidation().ValidateExecutable(_folder);
    [Fact] void should_admit_public_event_binding() => _result.Executable.ShouldEqual(true);
    [Fact] void should_have_no_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    void Destroy() => Directory.Delete(_folder, true);
}
