// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_RenderingDocumentation;

public class when_rendering_the_documented_example : given.the_rendering_documentation
{
    const string Heading = "### A minimal render";

    string _folder = null!;
    ScreenplayRenderPlan _plan = null!;
    string[] _documentedPackages = [];
    string _generatedPackages = string.Empty;

    async Task Because()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-render-docs-{Guid.NewGuid():N}")).FullName;
        var file = Path.Combine(_folder, "greetings.play");
        await File.WriteAllTextAsync(file, FencedBlock(ScreenplayPage, Heading, "play"), CancellationToken.None);
        _plan = await new ScreenplayPlanning().Plan(new(file, "Greetings", "cratis"), CancellationToken.None);
        _documentedPackages = FencedBlock(ScreenplayPage, Heading, "xml").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _generatedPackages = Content("Directory.Packages.props");
    }

    [Fact] void should_render_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_publish_the_documented_number_of_artifacts() => ScreenplayPage.ShouldContain($"The render publishes {_plan.Artifacts.Artifacts.Length} artifacts:");
    [Fact] void should_render_one_project_and_no_specifications_project() => _plan.Artifacts.Artifacts.Count(_ => _.RelativePath.EndsWith(".csproj", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_render_the_documented_slice_file() => _plan.Artifacts.Artifacts.Any(_ => _.RelativePath == "Greetings/Welcome/GreetCustomer/GreetCustomer.cs").ShouldBeTrue();
    [Fact] void should_document_some_packages() => _documentedPackages.ShouldNotBeEmpty();
    [Fact] void should_pin_every_documented_package_in_the_generated_manifest() => _documentedPackages.Where(_ => !_generatedPackages.Contains(_, StringComparison.Ordinal)).ShouldBeEmpty();

    string Content(string path) => Encoding.UTF8.GetString(_plan.Artifacts.Artifacts.Single(_ => _.RelativePath == path).Bytes.AsSpan());

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
