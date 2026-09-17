// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateConstraints.when_evaluating;

public class a_toolchain_dependent_constraint : Specification
{
    IReadOnlyDictionary<string, ConstraintEvaluation> _evaluations = null!;

    void Because() => _evaluations = TemplateConstraints.Evaluate(new TemplateConfig
    {
        Name = "T",
        ShortName = "t",
        Constraints = new Dictionary<string, ConstraintConfig>
        {
            ["sdk"] = new() { Type = "sdk-version", Allowed = ["10.0.100"] },
            ["workload"] = new() { Type = "workload", Allowed = ["aspire"] },
            ["capability"] = new() { Type = "project-capability", Allowed = ["x"] }
        }
    });

    [Fact] void should_evaluate_sdk_version_as_unevaluatable_without_a_toolchain() => _evaluations["sdk"].ShouldEqual(ConstraintEvaluation.Unevaluatable);

    [Fact] void should_evaluate_workload_as_unevaluatable_without_a_toolchain() => _evaluations["workload"].ShouldEqual(ConstraintEvaluation.Unevaluatable);

    [Fact] void should_evaluate_project_capability_as_unevaluatable_without_a_toolchain() => _evaluations["capability"].ShouldEqual(ConstraintEvaluation.Unevaluatable);
}
