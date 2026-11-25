// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_CoverageMapFile;

public class when_parsing_a_bad_map : Specification
{
    Exception _garbage;
    Exception _truncated;
    Exception _unknownSchema;
    Exception _badIndex;

    void Because()
    {
        _garbage = Catch.Exception(() => CoverageMapFile.Parse("not json at all"));
        _truncated = Catch.Exception(() => CoverageMapFile.Parse("{\"schemaVersion\":1,\"builtAt\":\"abc\""));
        _unknownSchema = Catch.Exception(() => CoverageMapFile.Parse("{\"schemaVersion\":99,\"builtAt\":\"a\",\"project\":\"p\",\"granularity\":\"class\",\"files\":[],\"units\":[]}"));
        _badIndex = Catch.Exception(() => CoverageMapFile.Parse("{\"schemaVersion\":1,\"builtAt\":\"a\",\"project\":\"p\",\"granularity\":\"class\",\"files\":[\"a.cs\"],\"units\":[{\"name\":\"U\",\"covers\":[5],\"declaredIn\":[]}]}"));
    }

    [Fact] void should_reject_garbage() => _garbage.ShouldBeOfExactType<AffectedException>();
    [Fact] void should_reject_truncated_text() => _truncated.ShouldBeOfExactType<AffectedException>();
    [Fact] void should_reject_an_unknown_schema_version() => _unknownSchema.Message.ShouldContain("schema version 99");
    [Fact] void should_reject_an_index_outside_the_file_table() => _badIndex.ShouldBeOfExactType<AffectedException>();
}
