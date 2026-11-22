// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Fixture = Cratis.Cli.for_ScreenplayPlanning.when_carrying_semantic_exclusions.given.an_excluded_slice;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_real_semantic_exclusions : given.a_check_command
{
    void Establish()
    {
        File.WriteAllText(_document, Fixture.Source);
        _settings.Path = _document;
        _settings.Name = "Projects";
        _settings.Check = false;
        _command = new RenderCommand();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_publish_the_admissible_artifacts_before_returning_acceptance(bool strict)
    {
        _settings.Strict = strict;
        var output = await Capture();
        output.ExitCode.ShouldEqual(strict ? ExitCodes.StrictUnaccepted : ExitCodes.Success);
        if (strict)
        {
            File.Exists(Path.Combine(_destination, ".stage", "completeness.json")).ShouldBeTrue();
        }
        var report = await File.ReadAllTextAsync(Path.Combine(_destination, "UNSUPPORTED.md"), TestContext.Current.CancellationToken);
        report.ShouldContain("Projects/Registration/Removed");
        report.ShouldContain("PLAY0268");
        var command = Directory.GetFiles(_destination, "RegisterProject.cs", SearchOption.AllDirectories).Single();
        (await File.ReadAllTextAsync(command, TestContext.Current.CancellationToken)).ShouldContain("RegisterProject");
        var sources = await Task.WhenAll(Directory.GetFiles(_destination, "*.cs", SearchOption.AllDirectories).Select(path => File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)));
        sources.Any(source => source.Contains("// Differs from Screenplay: Projects/Registration/Removed excluded (PLAY0268)", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
