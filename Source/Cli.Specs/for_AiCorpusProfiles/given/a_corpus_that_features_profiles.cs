// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusProfiles.given;

public class a_corpus_that_features_profiles : a_corpus
{
    protected const string ApplicationDescription = "Build an application on Cratis: model a business flow, implement its backend and UI slices, run and diagnose them.";
    protected const string EngineeringDescription = "Change a Cratis platform repository: follow its library contracts, verify the change and prepare the PR and release note.";

    void Establish()
    {
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), """{"harnesses":[],"profiles":["cratis/application","cratis/application/csharp","cratis/arc/core","cratis/engineering"],"languages":["csharp"]}""");
        WriteCatalog($$"""
            {"publicProfiles":[{"id":"cratis/application","description":"{{ApplicationDescription}}","featured":true,"composes":["cratis/arc/core"]},{"id":"cratis/application/csharp","composes":["cratis/arc/core"],"languages":["csharp"]},{"id":"cratis/arc/core","availableTargets":["cratis-arc-command"]}],"engineeringProfiles":[{"id":"cratis/engineering","description":"{{EngineeringDescription}}","featured":true}]}
            """);
    }
}
