// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_checking_publication : given.an_axon_target
{
    RenderPublicationRefusal? _refusal;
    bool _read;

    void Because() => _refusal = _target.CheckPublication(_plan, _ =>
    {
        _read = true;
        return "// Not C# event-type metadata";
    });

    [Fact] void should_not_apply_cratis_compatibility_rules() => _refusal.ShouldBeNull();
    [Fact] void should_not_read_files_for_cratis_metadata() => _read.ShouldBeFalse();
}
