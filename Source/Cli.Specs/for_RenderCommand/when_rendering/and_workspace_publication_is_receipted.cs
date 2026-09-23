// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_workspace_publication_is_receipted : given.a_workspace_render_command
{
    [Theory]
    [InlineData("single", OutputFormats.Json)]
    [InlineData("folder", OutputFormats.Json)]
    [InlineData("single", OutputFormats.JsonCompact)]
    [InlineData("folder", OutputFormats.JsonCompact)]
    [InlineData("single", OutputFormats.JsonQuiet)]
    [InlineData("folder", OutputFormats.JsonQuiet)]
    public async Task should_receipt_exact_imported_artifacts_and_an_unchanged_rerender(string form, string format)
    {
        WriteWorkspace(CreateWorkspace(form));
        _settings.Output = format;
        _settings.ProjectName = "Delivery.Backend";
        var inputBytes = await File.ReadAllBytesAsync(_input);

        using var first = JsonDocument.Parse(await Capture());

        var root = first.RootElement;
        var artifactCount = root.GetProperty("artifacts").GetInt32();
        artifactCount.ShouldBeGreaterThan(0);
        root.GetProperty("written").GetInt32().ShouldEqual(artifactCount);
        root.GetProperty("removed").GetInt32().ShouldEqual(0);
        root.GetProperty("unchanged").GetInt32().ShouldEqual(0);
        root.GetProperty("recovered").GetBoolean().ShouldBeFalse();
        var receipt = root.GetProperty("publication");
        receipt.GetProperty("schemaVersion").GetString().ShouldEqual("1");
        receipt.GetProperty("status").GetString().ShouldEqual("published");
        var changes = receipt.GetProperty("changes").EnumerateArray().ToArray();
        changes.Length.ShouldEqual(artifactCount);
        foreach (var change in changes)
        {
            change.GetProperty("kind").GetString().ShouldEqual("write");
            change.TryGetProperty("beforeSha256", out _).ShouldBeFalse();
            var path = Path.Combine(_destination, change.GetProperty("path").GetString()!);
            change.GetProperty("afterSha256").GetString().ShouldEqual(ArtifactPublicationStorage.Hash(path));
        }

        var manifestPath = ArtifactPublicationStorage.ManifestPath(_destination);
        var manifestBytes = await File.ReadAllBytesAsync(manifestPath);
        var manifestHash = ArtifactPublicationStorage.Hash(manifestBytes);
        var manifest = receipt.GetProperty("manifest");
        manifest.GetProperty("path").GetString().ShouldEqual(".cratis-render.json");
        manifest.TryGetProperty("baseSha256", out _).ShouldBeFalse();
        manifest.GetProperty("sha256").GetString().ShouldEqual(manifestHash);

        using var second = JsonDocument.Parse(await Capture());

        var rerender = second.RootElement;
        rerender.GetProperty("written").GetInt32().ShouldEqual(0);
        rerender.GetProperty("removed").GetInt32().ShouldEqual(0);
        rerender.GetProperty("unchanged").GetInt32().ShouldEqual(artifactCount);
        rerender.GetProperty("recovered").GetBoolean().ShouldBeFalse();
        var unchanged = rerender.GetProperty("publication");
        unchanged.GetProperty("status").GetString().ShouldEqual("published");
        unchanged.GetProperty("changes").GetArrayLength().ShouldEqual(0);
        unchanged.GetProperty("manifest").GetProperty("baseSha256").GetString().ShouldEqual(manifestHash);
        unchanged.GetProperty("manifest").GetProperty("sha256").GetString().ShouldEqual(manifestHash);
        (await File.ReadAllBytesAsync(manifestPath)).SequenceEqual(manifestBytes).ShouldBeTrue();
        (await File.ReadAllBytesAsync(_input)).SequenceEqual(inputBytes).ShouldBeTrue();
    }

    async Task<string> Capture()
    {
        var previousOutput = Console.Out;
        var previousError = Console.Error;
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            (await Execute()).ShouldEqual(ExitCodes.Success);
            error.ToString().ShouldEqual(string.Empty);
            return output.ToString();
        }
        finally
        {
            Console.SetOut(previousOutput);
            Console.SetError(previousError);
        }
    }
}
