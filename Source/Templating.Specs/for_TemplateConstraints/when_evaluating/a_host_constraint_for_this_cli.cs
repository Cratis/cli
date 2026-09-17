// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateConstraints.when_evaluating;

public class a_host_constraint_for_this_cli : Specification
{
    IReadOnlyDictionary<string, ConstraintEvaluation> _evaluations = null!;

    void Because() => _evaluations = TemplateConstraints.Evaluate(new TemplateConfig
    {
        Name = "T",
        ShortName = "t",
        Constraints = new Dictionary<string, ConstraintConfig>
        {
            ["host"] = new() { Type = "host", Allowed = ["cratiscli"] }
        }
    });

    [Fact] void should_be_satisfied_for_our_host_name() => _evaluations["host"].ShouldEqual(ConstraintEvaluation.Satisfied);
}
