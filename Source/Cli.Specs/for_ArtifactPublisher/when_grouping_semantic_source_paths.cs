// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_grouping_semantic_source_paths : Specification
{
    IReadOnlyList<ArtifactSourcePaths> _result = [];

    void Because() => _result = new ArtifactPublicationReceipt(
        [new("z.cs", "write", null, "hash") { Sources = [new("b", "command", "Same"), new("a", "event", "Same"), new("unknown", "unknown", null)] },
         new("a.cs", "delete", "hash", null) { Sources = [new("b", "command", "Same")] },
         new("z.cs", "write", null, "hash") { Sources = [new("b", "command", "Same")] }],
        new(null, "manifest-hash")).BySource;

    [Fact] void should_sort_equal_addresses_by_id_and_unknown_last() => _result.Select(source => source.Id).ShouldEqual(["a", "b", "unknown"]);
    [Fact] void should_sort_and_deduplicate_paths() => _result.Single(source => source.Id == "b").Paths.ShouldEqual<string>(["a.cs", "z.cs"]);
    [Fact] void should_preserve_an_unknown_source() => _result[^1].Address.ShouldBeNull();
}
