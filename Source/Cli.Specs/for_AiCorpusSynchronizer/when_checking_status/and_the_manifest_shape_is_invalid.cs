// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_the_manifest_shape_is_invalid : given.an_ownership_plan
{
    void Because()
    {
        foreach (var manifest in new[]
        {
            "null", "[]", "{}", "{",
            """{"SourceRevision":"test","Files":null}""",
            """{"SourceRevision":"test","Files":{}}""",
            """{"SourceRevision":"test","Files":[null]}""",
            """{"SourceRevision":"test","Files":[{}]}""",
            """{"SourceRevision":"test","Files":[{"Source":"rules/general.md","Destination":null,"Hash":"hash"}]}""",
            """{"SourceRevision":"test","Files":[],"Integrations":{}}"""
        })
        {
            File.WriteAllText(ManifestPath, manifest);
            ExerciseAllCommands();
        }
    }

    [Fact] void should_reject_all_shapes_with_a_validation_error() => _outcomes.TrueForAll(outcome => outcome.Error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_cover_every_shape_and_command() => _outcomes.Count.ShouldEqual(90);
    [Fact] void should_not_access_destinations() => _outcomes.TrueForAll(outcome => outcome.Accesses == 0).ShouldBeTrue();
}
