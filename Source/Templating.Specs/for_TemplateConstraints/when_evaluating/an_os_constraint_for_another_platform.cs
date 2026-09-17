// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateConstraints.when_evaluating;

public class an_os_constraint_for_another_platform : Specification
{
    IReadOnlyDictionary<string, ConstraintEvaluation> _evaluations = null!;

    void Because() => _evaluations = TemplateConstraints.Evaluate(new TemplateConfig
    {
        Name = "T",
        ShortName = "t",
        Constraints = new Dictionary<string, ConstraintConfig>
        {
            ["os"] = new() { Type = "os", Allowed = ["Windows"] }
        }
    });

    [Fact] void should_be_unsatisfied_on_non_windows() =>
        _evaluations["os"].ShouldEqual(OperatingSystem.IsWindows() ? ConstraintEvaluation.Satisfied : ConstraintEvaluation.Unsatisfied);
}
