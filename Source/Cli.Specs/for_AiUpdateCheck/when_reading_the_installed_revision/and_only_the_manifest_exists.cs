// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_reading_the_installed_revision;

/// <summary>
/// Without .cratis/ai.json 'cratis ai update' has no selection to update, so there is nothing to suggest.
/// </summary>
public class and_only_the_manifest_exists : given.a_project
{
    string? _result;

    void Establish()
    {
        Write(".cratis/ai.manifest.json", "{\"SourceRevision\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"Files\":[]}");
    }

    void Because() => _result = AiUpdateCheck.InstalledRevision(_project);

    [Fact] void should_report_nothing_installed() => _result.ShouldBeNull();
}
