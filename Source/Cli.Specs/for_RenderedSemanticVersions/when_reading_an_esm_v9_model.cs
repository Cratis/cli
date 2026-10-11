// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Cli.for_RenderedSemanticVersions;

public class when_reading_an_esm_v9_model : Specification
{
    ExecutableSemanticModel _model = null!;
    ScreenplayDiagnostic? _diagnostic;

    void Because()
    {
        _model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        _diagnostic = RenderedSemanticVersions.Check(_model.SemanticVersion);
    }

    [Fact] void should_strict_read_schema_nine() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V9);
    [Fact] void should_preserve_the_canonical_bytes() => SemanticModelSerializer.Serialize(_model).SequenceEqual(PublicEventsCorpus.V9.EsmBytes).ShouldBeTrue();
    [Fact] void should_admit_the_version_for_renderer_construct_checks() => _diagnostic.ShouldBeNull();
}
