// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_the_root_reader_rejects_a_document : given.a_model_root
{
    void Establish() => _check = new(_root, _ => throw new DocumentRejected());
    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_report_unavailable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unavailable);
    [Fact] void should_keep_the_reason() => _assessment.Reason.ShouldEqual("The document is not valid UTF-8.");

    sealed class DocumentRejected() : Exception("The document is not valid UTF-8.");
}
