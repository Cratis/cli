// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;
using Cratis.Templating.Specs.for_PostActionRunner.given;

namespace Cratis.Templating.Specs.for_PostActionRunner;

public class when_instructions_are_displayed : a_runner
{
    IReadOnlyList<PostActionResult>? _results;

    void Because() => _results = Runner().Run(
        new TemplateConfig
        {
            Name = "T",
            ShortName = "t",
            PostActions = [Action("AC1156F7-BB77-4DB8-B28F-24EEBCCA1E5C")]
        },
        ResultFor(Path.GetTempPath()),
        new Dictionary<string, string>()).Result;

    [Fact] void should_report_them_as_displayed() => _results![0].Outcome.ShouldEqual(PostActionOutcome.Displayed);
}
