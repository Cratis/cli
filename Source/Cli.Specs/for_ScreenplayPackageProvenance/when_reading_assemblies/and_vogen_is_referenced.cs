// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ScreenplayPackageProvenance.when_reading_assemblies;

public class and_vogen_is_referenced : Specification
{
    IReadOnlyList<ScreenplayAssemblyIdentity> _result;

    void Because()
    {
        var compilation = CSharpCompilation.Create(
            "Application",
            references:
            [
                ReferenceTo("Vogen", "8.0.7.0"),
                ReferenceTo("Cratis.Screenplay.Generation.DotNet.Vogen", "0.7.0.0")
            ]);

        _result = ScreenplayPackageProvenance.AssembliesFrom(compilation);
    }

    [Fact] void should_include_the_application_vogen_assembly() => _result.ShouldContain(new ScreenplayAssemblyIdentity("Vogen", "8.0.7.0"));
    [Fact] void should_not_include_the_bundled_vogen_adapter_assembly() => _result.ShouldNotContain(new ScreenplayAssemblyIdentity("Cratis.Screenplay.Generation.DotNet.Vogen", "0.7.0.0"));

    static MetadataReference ReferenceTo(string assemblyName, string version)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText($"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]")],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        compilation.Emit(stream).Success.ShouldBeTrue();

        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}
