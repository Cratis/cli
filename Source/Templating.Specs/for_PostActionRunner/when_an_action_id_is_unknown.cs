// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;
using Cratis.Templating.Specs.for_PostActionRunner.given;

namespace Cratis.Templating.Specs.for_PostActionRunner;

public class when_an_action_id_is_unknown : a_runner
{
    IReadOnlyList<PostActionResult>? _results;

    void Because() => _results = Runner().Run(
        new TemplateConfig { Name = "T", ShortName = "t", PostActions = [Action("00000000-0000-0000-0000-000000000000")] },
        ResultFor(Path.GetTempPath()),
        new Dictionary<string, string>()).Result;

    [Fact] void should_report_the_action_as_unknown() => _results![0].Outcome.ShouldEqual(PostActionOutcome.Unknown);
    [Fact] void should_carry_the_manual_instructions() => _results![0].Instructions.ShouldContain("Do it manually.");
}
