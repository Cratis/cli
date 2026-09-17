// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;
using Cratis.Templating.Specs.for_PostActionRunner.given;

namespace Cratis.Templating.Specs.for_PostActionRunner;

public class when_a_required_action_fails : a_runner
{
    IReadOnlyList<PostActionResult>? _results;

    void Because() => _results = Runner().Run(
        new TemplateConfig
        {
            Name = "T",
            ShortName = "t",

            // Add-reference fails when no project file exists in the output.
            PostActions = [Action("B17581D1-C5C9-4489-8F0A-004BE667B814", args: new Dictionary<string, string>
            {
                ["referenceType"] = "package",
                ["reference"] = "Some.Package"
            })]
        },
        ResultFor(Path.Combine(Path.GetTempPath(), "cratis-postaction-specs", Guid.NewGuid().ToString("N"))),
        new Dictionary<string, string>()).Result;

    [Fact] void should_report_the_failure() => _results![0].Outcome.ShouldEqual(PostActionOutcome.Failed);
    [Fact] void should_mark_it_as_a_run_failure() => _results![0].IsFailure.ShouldBeTrue();
}
