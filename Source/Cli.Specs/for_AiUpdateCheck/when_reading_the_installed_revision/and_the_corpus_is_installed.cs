// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_reading_the_installed_revision;

public class and_the_corpus_is_installed : given.a_project
{
    string? _result;

    void Establish()
    {
        Write(".cratis/ai.json", "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/documentation\"]}");
        Write(".cratis/ai.manifest.json", "{\"SourceRevision\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"Files\":[]}");
    }

    void Because() => _result = AiUpdateCheck.InstalledRevision(_project);

    [Fact] void should_read_the_recorded_revision() => _result.ShouldEqual("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
}
