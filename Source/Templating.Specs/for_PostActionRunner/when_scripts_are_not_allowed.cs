// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;
using Cratis.Templating.Specs.for_PostActionRunner.given;

namespace Cratis.Templating.Specs.for_PostActionRunner;

public class when_scripts_are_not_allowed : a_runner
{
    IReadOnlyList<PostActionResult>? _results;

    void Because() => _results = Runner(ScriptPolicy.Deny).Run(
        new TemplateConfig
        {
            Name = "T",
            ShortName = "t",
            PostActions = [Action("3A7C4B45-1F5D-4A30-959A-51B88E82B5D2", args: new Dictionary<string, string>
            {
                ["executable"] = "yarn",
                ["args"] = "install"
            })]
        },
        ResultFor(Path.GetTempPath()),
        new Dictionary<string, string>()).Result;

    [Fact] void should_decline_the_script() => _results![0].Outcome.ShouldEqual(PostActionOutcome.Denied);
    [Fact] void should_report_the_manual_instructions() => _results![0].Instructions.ShouldContain("Do it manually.");
}
