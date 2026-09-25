// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments;

public class from_a_single_file : given.a_model_root
{
    const string Body = "return context.Value > 0;\r\n";
    ScreenplayRenderPlan _result = null!;

    void Establish()
    {
        WriteSource();
        WriteAttachment(Body);
    }

    async Task Because() => _result = await _planning.Plan(new(Path.GetRelativePath(Directory.GetCurrentDirectory(), _path), "Orders", "cratis"), CancellationToken.None);

    [Fact] void should_load_from_the_parent_of_a_relative_path() => _supplied!.AttachmentContents["Rules/Positive.cs"].ShouldEqual(Body);
    [Fact] void should_supply_the_body_to_stage() => _renderRequest!.ImplementationContents.Single().Value.ShouldEqual(Body);
    [Fact] void should_report_the_unsupported_v3_validation() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-005");
    [Fact] void should_not_report_an_unresolved_body() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain("STAGE-ESM-020");
}
