// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_publication_receipts_are_reported : given.a_render_command
{
    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    public async Task should_forward_versioned_receipt_with_stable_strings_and_omitted_absent_hashes(string format)
    {
        _settings.Output = format;
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>()).Returns(Published(false));

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.Success);
        output.Stderr.ShouldEqual(string.Empty);
        using var json = JsonDocument.Parse(output.Stdout);
        var root = json.RootElement;
        root.GetProperty("written").GetInt32().ShouldEqual(2);
        root.GetProperty("removed").GetInt32().ShouldEqual(1);
        root.GetProperty("unchanged").GetInt32().ShouldEqual(4);
        root.GetProperty("recovered").GetBoolean().ShouldBeFalse();
        var receipt = root.GetProperty("publication");
        receipt.GetProperty("schemaVersion").GetString().ShouldEqual("1");
        receipt.GetProperty("status").GetString().ShouldEqual("published");
        var changes = receipt.GetProperty("changes").EnumerateArray().ToArray();
        changes.Select(change => change.GetProperty("path").GetString()).ShouldEqual("new.cs", "replace.cs", "delete.cs");
        changes.Select(change => change.GetProperty("kind").GetString()).ShouldEqual("write", "write", "delete");
        changes[0].TryGetProperty("beforeSha256", out _).ShouldBeFalse();
        changes[0].GetProperty("afterSha256").GetString().ShouldEqual("new-hash");
        changes[1].GetProperty("beforeSha256").GetString().ShouldEqual("before-hash");
        changes[1].GetProperty("afterSha256").GetString().ShouldEqual("after-hash");
        changes[2].GetProperty("beforeSha256").GetString().ShouldEqual("stale-hash");
        changes[2].TryGetProperty("afterSha256", out _).ShouldBeFalse();
        var manifest = receipt.GetProperty("manifest");
        manifest.GetProperty("path").GetString().ShouldEqual(".cratis-render.json");
        manifest.TryGetProperty("baseSha256", out _).ShouldBeFalse();
        manifest.GetProperty("sha256").GetString().ShouldEqual("manifest-hash");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task should_report_recovery_from_either_boundary(bool beforePlanning, bool insidePublication)
    {
        _publication.Recover(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(beforePlanning);
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>()).Returns(Published(insidePublication));

        var output = await Capture();

        using var json = JsonDocument.Parse(output.Stdout);
        json.RootElement.GetProperty("recovered").GetBoolean().ShouldEqual(beforePlanning || insidePublication);
        Received.InOrder(() =>
        {
            _publication.Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
            _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>());
            _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task should_keep_quiet_output_as_only_the_destination()
    {
        _settings.Output = OutputFormats.Quiet;
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>()).Returns(Published(false));

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.Success);
        output.Stdout.ShouldEqual(Path.Combine(_folder, RenderCommand.DefaultDestination) + Environment.NewLine);
        output.Stderr.ShouldEqual(string.Empty);
    }

    [Fact]
    public async Task should_not_emit_a_receipt_when_planning_fails_after_recovery()
    {
        _publication.Recover(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>()).Returns(new ScreenplayRenderPlan(
            1, [new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "PLAY0001", "invalid model", "Model.play(1,1)")], null));

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
        output.Stdout.ShouldEqual(string.Empty);
        output.Stderr.ShouldContain("PLAY0001");
        output.Stderr.ShouldNotContain("\"publication\"");
        await _publication.Received(1).Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
    }

    static ArtifactPublicationResult Published(bool recovered) => new(
        2,
        1,
        4,
        new(
            [new("new.cs", "write", null, "new-hash"), new("replace.cs", "write", "before-hash", "after-hash"), new("delete.cs", "delete", "stale-hash", null)],
            new(null, "manifest-hash")),
        recovered);

    async Task<(int ExitCode, string Stdout, string Stderr)> Capture()
    {
        var previousOutput = Console.Out;
        var previousError = Console.Error;
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = await Execute();
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(previousOutput);
            Console.SetError(previousError);
        }
    }
}
