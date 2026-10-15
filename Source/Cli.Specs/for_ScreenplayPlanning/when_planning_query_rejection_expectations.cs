// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_query_rejection_expectations : given.a_screenplay_planning
{
    static readonly string QueryRejectionSource = string.Join('\n',
        "concept ProjectCode : String",
        "  validate",
        "    min 2",
        "module Projects",
        "  feature Registration",
        "    slice StateChange Register",
        "      command RegisterProject",
        "        code ProjectCode identifier",
        "        produces ProjectRegistered",
        "          for code",
        "          code = code",
        "      event ProjectRegistered",
        "        code ProjectCode",
        "    slice StateView Lookup",
        "      readmodel ProjectSummary",
        "        code ProjectCode",
        "      query ProjectByCode => ProjectSummary optional",
        "        by code ProjectCode",
        "      projection ProjectSummaryProjection => ProjectSummary",
        "        from ProjectRegistered key code",
        "          code = code",
        "      specification RejectingInvalidCode",
        "        when query ProjectByCode",
        "          code = \"x\"",
        "        then error");

    ScreenplayRenderPlan _result = null!;
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        File.WriteAllText(_file, QueryRejectionSource);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("query-rejections"), "query-rejections", "QueryRejections.play", QueryRejectionSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        _model = compilation.Value!.Model;
    }

    async Task Because() => _result = await Plan();

    [Fact] void should_compile_to_v18() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V18);
    [Fact] void should_admit_v18() => RenderedSemanticVersions.Check(_model.SemanticVersion).ShouldBeNull();
    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_report_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_render_the_query_validation_rejection() => SpecificationCode.ShouldContain("should_reject_the_query() => _result.IsValid.ShouldBeFalse()");
    [Fact] void should_render_the_validation_failure_assertion() => SpecificationCode.ShouldContain("should_report_validation_failures() => _result.ValidationResults.ShouldNotBeEmpty()");

    string SpecificationCode => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_rejecting_invalid_code_is_queried.cs", StringComparison.Ordinal)).Bytes.AsSpan());
}
