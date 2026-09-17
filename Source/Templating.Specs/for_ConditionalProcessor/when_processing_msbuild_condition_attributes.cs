// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Expressions;
using Cratis.Templating.Processing;
using Cratis.Templating.Specs.for_ConditionalProcessor.given;

namespace Cratis.Templating.Specs.for_ConditionalProcessor;

public class when_processing_msbuild_condition_attributes : a_processor
{
    readonly IReadOnlyDictionary<string, string> _scope = new Dictionary<string, string> { ["TargetFrameworkOverride"] = string.Empty };
    string _result = null!;

    void Because() => _result = ConditionalProcessor.ProcessMsBuildConditions("        <Project>\n          <TargetFramework Condition=\"'$(TargetFrameworkOverride)' == ''\">net10.0</TargetFramework>\n          <TargetFramework Condition=\"'$(TargetFrameworkOverride)' != ''\">overridden</TargetFramework>\n        </Project>",
        _scope,
        "test.csproj");

    [Fact] void should_keep_the_satisfied_element_without_the_condition() => _result.ShouldContain("<TargetFramework>net10.0</TargetFramework>");
    [Fact] void should_remove_the_unsatisfied_element() => _result.ShouldNotContain("overridden");
}
