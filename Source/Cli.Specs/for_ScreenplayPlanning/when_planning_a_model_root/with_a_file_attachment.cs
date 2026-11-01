// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class with_a_file_attachment : given.a_persisted_model_root
{
    const string Body = "return context.Identity.IsAuthenticated;\n";

    protected override string ModelSource => "policy SavedAccess\n  file Bodies/Access.cs\n" + base.ModelSource.Replace("command Observe\n", "command Observe\n        authorize SavedAccess\n", StringComparison.Ordinal);

    void Establish() => File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(_root, "Bodies")).FullName, "Access.cs"), Body);
    async Task Because() => _result = await _planning.Plan(new(_root, "Model", "cratis"), CancellationToken.None);

    [Fact] void should_succeed() => Assert.True(_result.Success, string.Join(Environment.NewLine, _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    [Fact] void should_use_the_roots_loaded_attachment() => OpenWorkspace().AttachmentContents["Bodies/Access.cs"].ShouldEqual(Body);
    [Fact] void should_supply_the_exact_body_to_stage() => _requests.Single().ImplementationContents.Values.ShouldContain(Body);
    [Fact] void should_preserve_the_compilation_revision_with_attachments() => _result.Artifacts!.SemanticRevision.ShouldEqual(OpenWorkspace().Compilation.Value!.Model.Revision);
    [Fact] void should_not_report_a_missing_body() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-020");
}
