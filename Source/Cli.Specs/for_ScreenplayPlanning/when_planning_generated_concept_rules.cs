// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_generated_concept_rules : given.a_screenplay_planning
{
    [Theory]
    [InlineData("error")]
    [InlineData("warning")]
    [InlineData("information")]
    public async Task should_render_a_rule_of_every_severity_with_the_factory_and_validator(string severity)
    {
        var source = string.Join('\n',
            "concept CustomerId : String",
            "  generate ulid prefix \"cus_\" case lower",
            "  validate",
            $"    matches \"^cus_[0-7][0-9a-hjkmnp-tv-z]{{25}}$\" severity {severity}",
            "module Shop",
            "  feature Customers",
            "    slice StateChange Register",
            "      command Register",
            "        id CustomerId generated identifier",
            "        produces event Registered",
            "        returns id",
            "      specification Registering",
            "        when Register",
            "          for \"cus_01arz3ndektsv4rrffq69g5fav\"",
            "        then Registered");
        await File.WriteAllTextAsync(_file, source, TestContext.Current.CancellationToken);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("generated-rules"), "generated-rules", "GeneratedRules.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V20);

        var result = await Plan();

        result.Diagnostics.Any(diagnostic => diagnostic.Code == RenderedSemanticVersions.NotAdmittedCode).ShouldBeFalse();
        result.Success.ShouldBeTrue();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
        var concept = Encoding.UTF8.GetString(result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath == "Common/CustomerId.cs").Bytes.AsSpan());
        concept.ShouldContain("New(");
        concept.ShouldContain("ConceptValidator<CustomerId>");
        concept.ShouldContain("PortableMatcher.Compile(");
    }
}
