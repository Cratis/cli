// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ScreenplayPlanning.when_mapping_source_lines;

public class with_reachable_sources : given.an_inline_model
{
    string _destination = null!;

    async Task Because()
    {
        _destination = Path.Combine(_folder, "output");
        _result = await _planning.Plan(new(_root, "Orders", "cratis") { Destination = _destination }, CancellationToken.None);
    }

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_emit_line_directives() => RenderedCode().ShouldContain(code => code.Contains("#line 23 ", StringComparison.Ordinal));
    [Fact] void should_restore_default_line_mapping() => RenderedCode().ShouldContain(code => code.Contains("#line default", StringComparison.Ordinal));

    [Fact]
    void should_report_the_compiler_error_at_the_authored_file_and_line()
    {
        var artifact = _result.Artifacts.Artifacts.Single(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains(Body, StringComparison.Ordinal));
        var generatedPath = Path.Combine(_destination, artifact.RelativePath);

        // Stage type-checks pure bodies before emission; inject the compiler error after rendering.
        var invalidBody = Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace(Body, "return DoesNotExist;", StringComparison.Ordinal);
        var tree = CSharpSyntaxTree.ParseText(invalidBody, path: generatedPath, cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("InlineError", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)], new(OutputKind.DynamicallyLinkedLibrary));
        var diagnostic = compilation.GetDiagnostics(TestContext.Current.CancellationToken).Single(diagnostic => diagnostic.Id == "CS0103" && diagnostic.GetMessage().Contains("DoesNotExist", StringComparison.Ordinal));
        var mapped = diagnostic.Location.GetMappedLineSpan();
        Path.IsPathRooted(mapped.Path).ShouldBeFalse();
        Path.GetFullPath(mapped.Path, Path.GetDirectoryName(generatedPath)!).ShouldEqual(_file);
        mapped.StartLinePosition.Line.ShouldEqual(22);
        File.Exists(Path.GetFullPath(mapped.Path, Path.GetDirectoryName(generatedPath)!)).ShouldBeTrue();
    }
}
