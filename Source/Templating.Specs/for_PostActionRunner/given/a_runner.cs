// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.given;

public abstract class a_runner : Specification
{
    protected static PostActionRunner Runner(ScriptPolicy policy = ScriptPolicy.Deny) => new(scriptPolicy: policy);

    protected static InstantiationResult ResultFor(string outputRoot, params string[] primaryOutputs) => new(
        "MyApp",
        outputRoot,
        [],
        [.. primaryOutputs.Select(path => Path.Combine(outputRoot, path))],
        new Dictionary<string, string>(),
        []);

    protected static PostActionConfig Action(
        string actionId,
        bool continueOnError = false,
        Dictionary<string, string>? args = null,
        string? condition = null) => new()
    {
        ActionId = actionId,
        ContinueOnError = continueOnError,
        Args = args ?? [],
        Condition = condition,
        ManualInstructions = [new() { Text = "Do it manually." }]
    };
}
