// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments;

public class with_a_linked_file : given.a_model_root
{
    ScreenplayRenderPlan _result = null!;

    void Establish()
    {
        WriteSource();
        Directory.CreateDirectory(Path.Combine(_root, "Rules"));
        File.WriteAllText(Path.Combine(_root, "real.cs"), "return true;");
        if (!OperatingSystem.IsWindows())
        {
            File.CreateSymbolicLink(Path.Combine(_root, "Rules", "Positive.cs"), Path.Combine(_root, "real.cs"));
        }
    }

    async Task Because()
    {
        if (!OperatingSystem.IsWindows())
        {
            _result = await _planning.Plan(new(_root, "Orders", "cratis"), CancellationToken.None);
        }
    }

    [Fact] void should_refuse_the_link() { if (!OperatingSystem.IsWindows()) _result.Diagnostics.Select(_ => _.Code).ShouldContain("PLAY0431"); }
    [Fact] void should_block_the_body_with_the_refusal_reason() { if (!OperatingSystem.IsWindows()) _result.Diagnostics.Any(_ => _.Code == "STAGE-ESM-020" && _.Message.Contains("PLAY0431", StringComparison.Ordinal)).ShouldBeTrue(); }
}
