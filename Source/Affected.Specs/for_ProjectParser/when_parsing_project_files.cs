// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_ProjectParser;

public class when_parsing_project_files : Specification
{
    ProjectDefinition _library;
    ProjectDefinition _xunit;
    ProjectDefinition _testSdk;
    ProjectDefinition _flagged;
    ProjectDefinition _generator;
    ProjectDefinition _linked;
    ProjectDefinition _declaredNot;
    ProjectDefinition _fixture;

    void Because()
    {
        var configuration = new AffectedConfiguration();
        _library = ProjectParser.Parse("Source/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"..\\Core\\Core.csproj\" /></ItemGroup></Project>", configuration);
        _xunit = ProjectParser.Parse("Source/A.Specs/A.Specs.csproj", "<Project><ItemGroup><PackageReference Include=\"xunit.v3.mtp-off\" /></ItemGroup></Project>", configuration);
        _testSdk = ProjectParser.Parse("Source/B.Specs/B.Specs.csproj", "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup></Project>", configuration);
        _flagged = ProjectParser.Parse("Source/C/C.csproj", "<Project><PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup></Project>", configuration);
        _declaredNot = ProjectParser.Parse("Source/D/D.csproj", "<Project><PropertyGroup><IsTestProject>false</IsTestProject></PropertyGroup><ItemGroup><PackageReference Include=\"xunit.v3\" /></ItemGroup></Project>", configuration);
        _fixture = ProjectParser.Parse("Source/F/Fixtures/F.csproj", "<Project><ItemGroup><PackageReference Include=\"xunit.v3.mtp-off\" /></ItemGroup></Project>", new AffectedConfiguration { NonTestProjects = ["**/Fixtures/**"] });
        _generator = ProjectParser.Parse("Source/G/G.csproj", "<Project><PropertyGroup><IsRoslynComponent>true</IsRoslynComponent></PropertyGroup></Project>", configuration);
        _linked = ProjectParser.Parse(
            "Source/Linked/Linked.csproj",
            "<Project><ItemGroup><Compile Include=\"..\\Shared\\Common.cs\" /><Compile Include=\"..\\Shared2\\**\\*.cs\" /><Compile Include=\"Local\\*.cs\" /><None Include=\"..\\..\\..\\outside.txt\" /></ItemGroup><Import Project=\"..\\build\\extra.props\" /></Project>",
            configuration);
    }

    [Fact] void should_resolve_project_references_to_repository_paths() => _library.References.ShouldContainOnly("Source/Core/Core.csproj");
    [Fact] void should_treat_a_library_as_not_a_test_project() => _library.IsTestProject.ShouldBeFalse();
    [Fact] void should_recognize_xunit_v3_executables_as_test_projects() => _xunit.IsTestProject.ShouldBeTrue();
    [Fact] void should_recognize_the_test_sdk() => _testSdk.IsTestProject.ShouldBeTrue();
    [Fact] void should_recognize_the_is_test_project_property() => _flagged.IsTestProject.ShouldBeTrue();
    [Fact] void should_let_an_explicit_false_override_a_test_package() => _declaredNot.IsTestProject.ShouldBeFalse();
    [Fact] void should_let_configuration_exclude_a_fixture_that_references_a_test_package() => _fixture.IsTestProject.ShouldBeFalse();
    [Fact] void should_recognize_a_roslyn_component() => _generator.IsGenerator.ShouldBeTrue();
    [Fact] void should_record_files_linked_from_outside_the_project_directory() => _linked.ExternalInputs.ShouldContainOnly("Source/Shared/Common.cs", "Source/Shared2/", "Source/build/extra.props");
    [Fact] void should_not_record_files_inside_the_project_directory() => _linked.ExternalInputs.ShouldNotContain("Source/Linked/Local/");
}
