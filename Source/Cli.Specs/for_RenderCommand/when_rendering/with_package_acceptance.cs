// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_package_acceptance : given.a_package_acceptance_command
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_publish_safe_package_artifacts_before_returning_acceptance(bool strict)
    {
        _settings.Check = false;
        _settings.Strict = strict;

        var output = await Capture();

        output.ExitCode.ShouldEqual(strict ? ExitCodes.StrictUnaccepted : ExitCodes.Success);
        var plan = await PlanPackage();
        plan.Success.ShouldBeTrue();
        plan.Artifacts!.Strict.ShouldEqual(strict);
        plan.Artifacts.Success.ShouldEqual(!strict);
        foreach (var artifact in plan.Artifacts.Artifacts)
        {
            (await File.ReadAllBytesAsync(Path.Combine(_destination, artifact.RelativePath), TestContext.Current.CancellationToken)).ShouldEqual<byte>([.. artifact.Bytes]);
        }
        File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeTrue();
        if (strict) output.Stderr.ShouldContain(ExitCodes.StrictUnacceptedCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_keep_strict_checks_read_only_with_or_without_drift(bool published)
    {
        _settings.Strict = true;
        if (published)
        {
            var plan = await PlanPackage();
            await _publication.Publish(new(plan.Artifacts!, _destination, false), CancellationToken.None);
        }
        var before = Snapshot(_destination);

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.StrictUnaccepted);
        Snapshot(_destination).ShouldEqual(before);
        Directory.Exists(_destination).ShouldEqual(published);
        Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
        using var json = JsonDocument.Parse(output.Stdout);
        json.RootElement.GetProperty("publication").GetProperty("status").GetString().ShouldEqual("check");
        var hasPendingWrites = json.RootElement.GetProperty("written").GetInt32() > 0;
        hasPendingWrites.ShouldEqual(!published);
        json.RootElement.GetProperty("refused").GetInt32().ShouldEqual(0);
        output.Stderr.ShouldContain(ExitCodes.StrictUnacceptedCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_prioritize_original_generation_errors_over_acceptance(bool check)
    {
        _unsafeGeneration = true;
        _settings.Strict = true;
        _settings.Check = check;

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
        Directory.Exists(_destination).ShouldBeFalse();
    }
}
