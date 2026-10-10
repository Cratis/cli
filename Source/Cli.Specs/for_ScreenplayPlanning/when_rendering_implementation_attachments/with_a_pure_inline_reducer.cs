// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments;

public class with_a_pure_inline_reducer : given.a_model_root
{
    ScreenplayRenderPlan _result = null!;
    void Establish() => WriteSource(PureReducerSource);
    async Task Because() => _result = await _planning.Plan(new(_path, "Orders", "cratis"), CancellationToken.None);

    [Fact] void should_plan_a_publishable_render() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).Select(_ => $"{_.Code}: {_.Message}").ShouldBeEmpty();
    [Fact] void should_render_the_body_as_a_chronicle_reducer() => Reducer().ShouldContain("Cratis.Chronicle.Reducers.IReducerFor<");
    [Fact] void should_keep_the_body_unchanged() => Reducer().ShouldContain("return new Total(context.Event.Id, context.Event.Amount);");

    string Reducer() => _result.Artifacts!.Artifacts
        .Select(_ => System.Text.Encoding.UTF8.GetString(_.Bytes.AsSpan()))
        .Single(_ => _.Contains("Cratis.Chronicle.Reducers.IReducerFor<", StringComparison.Ordinal));
}
