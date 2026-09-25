// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments;

public class with_a_non_text_file : given.a_model_root
{
    ScreenplayRenderPlan _result = null!;

    void Establish()
    {
        WriteSource();
        Directory.CreateDirectory(Path.Combine(_root, "Rules"));
        File.WriteAllBytes(Path.Combine(_root, "Rules", "Positive.cs"), [0, 255, 0]);
    }

    async Task Because() => _result = await _planning.Plan(new(_root, "Orders", "cratis"), CancellationToken.None);

    [Fact] void should_warn_about_the_non_text_file() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("PLAY0434");
    [Fact] void should_block_the_body_with_the_refusal_reason() => _result.Diagnostics.Any(_ => _.Code == "STAGE-ESM-020" && _.Message.Contains("PLAY0434", StringComparison.Ordinal)).ShouldBeTrue();
}
